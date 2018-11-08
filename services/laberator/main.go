package main

import (
	"encoding/base64"
	"fmt"
	"github.com/gorilla/websocket"
	_ "github.com/jinzhu/gorm/dialects/postgres"
	"html/template"
	"io"
	"io/ioutil"
	"log"
	"net/http"
	"strconv"
	"strings"
)

const ConfigPath = "config"

var dbApi DBApi
var sm SessionManager
var executor = CommandExecutor{&dbApi, &sm}

func Redirect(w http.ResponseWriter, r *http.Request, url string) {
	fmt.Fprintf(w, `<html><head></head><body><script>window.location.replace("%v")</script></body></html>`, url)
}

func Register(w http.ResponseWriter, r *http.Request) {
	r.ParseForm()
	login := r.Form.Get("login")
	password := strings.Replace(r.Form.Get("password"), " ", "+", -1)
	phrase := strings.Replace(r.Form.Get("phrase"), " ", "+", -1)

	if !IsValidLogin(&login) || !IsValidPhrase(&phrase) || !IsValidPassword(&password) {
		w.WriteHeader(400)
		return
	}

	if dbApi.IsUserExist(&login) {
		w.WriteHeader(400)
		return
	}

	dbApi.Register(&login, &password, &phrase)
	cookies := sm.CreateSession(login)
	for _, cookie := range cookies {
		http.SetCookie(w, &cookie)
	}
	Redirect(w, r, "/")
	return
}

func Login(w http.ResponseWriter, r *http.Request) {
	r.ParseForm()
	login := r.Form.Get("login")
	password := strings.Replace(r.Form.Get("password"), " ", "+", -1)
	if !IsValidLogin(&login) || !IsValidPassword(&password) {
		w.WriteHeader(400)
	} else if !dbApi.Validate(&login, &password) {
		w.WriteHeader(400)
	} else {
		cookies := sm.CreateSession(login)
		for _, cookie := range cookies {
			http.SetCookie(w, &cookie)
		}
		Redirect(w, r, "/")
		return
	}
}

func Exec(w io.Writer, filename string, state *State) {
	t := template.New("")
	data, err := ioutil.ReadFile(filename)
	if err != nil {
		panic("Exec error: " + err.Error())
	}
	t.Parse(string(data))
	t.Execute(w, *state)
}

func Main(w http.ResponseWriter, r *http.Request) {
	ok, login := sm.ValidateSession(r.Cookies())
	var logged string
	if ok {
		logged = login
	} else {
		logged = ""
	}
	Exec(w, "templates/main.html", &State{Login: logged})
}

func PhrasePage(w http.ResponseWriter, r *http.Request) {
	ok, login := sm.ValidateSession(r.Cookies())
	if !ok {
		Redirect(w, r, "/")
		return
	}
	err, phrase := dbApi.GetPhrase(&login)
	if err != nil {
		Redirect(w, r, "/")
		return
	}
	decodedPhrase, err := base64.StdEncoding.DecodeString(strings.Replace(*phrase, " ", "+", -1))
	if err != nil {
		Redirect(w, r, "/")
		return
	}
	Exec(w, "templates/phrase.html", &State{Login: login, Phrase: string(decodedPhrase)})
}

func RegisterPage(w http.ResponseWriter, r *http.Request) {
	ok, _ := sm.ValidateSession(r.Cookies())
	if ok {
		Redirect(w, r, "/")
		return
	} else {
		Exec(w, "templates/register.html", &State{})
	}
}

func CreatePage(w http.ResponseWriter, r *http.Request) {
	ok, login := sm.ValidateSession(r.Cookies())
	if ok {
		Exec(w, "templates/create.html", &State{Login: login})
	} else {
		Redirect(w, r, "/")
		return
	}
}

func ListingPage(w http.ResponseWriter, r *http.Request) {
	ok, login := sm.ValidateSession(r.Cookies())
	if ok {
		Exec(w, "templates/listing.html", &State{Login: login})
	} else {
		Redirect(w, r, "/")
		return
	}
}

func ViewLabel(w http.ResponseWriter, r *http.Request) {
	labelId, err := strconv.ParseUint(r.URL.Path[len("/labels/"):], 10, 64)
	if err != nil {
		w.WriteHeader(400)
		return
	}
	ok, login := sm.ValidateSession(r.Cookies())
	if ok {
		if !dbApi.CheckLabelOwner(login, labelId) {
			w.WriteHeader(400)
		} else {
			Exec(w, "templates/view.html", &State{LabelId: labelId, Login: login})
		}
	} else {
		Redirect(w, r, "/")
		return
	}
}

var upgrader = websocket.Upgrader{} // use default options

func ProcessCommand(w http.ResponseWriter, r *http.Request) {
	c, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		log.Print("upgrade:", err)
		return
	}
	defer c.Close()
	for {
		mt, data, err := c.ReadMessage()
		if err != nil {
			log.Println("reading error:", err)
			break
		}
		result, err := executor.Execute(data)
		if err != nil {
			log.Println("executing error:", err)
			break
		}
		err = c.WriteMessage(mt, result)
		if err != nil {
			log.Println("writing error:", err)
			break
		}
	}
}

func Ws(w http.ResponseWriter, r *http.Request) {
	t := template.New("")
	t.ParseFiles("templates/ws.html")
	t.ExecuteTemplate(w, "url", "ws://"+r.Host+"/echo")
}

func Logout(w http.ResponseWriter, r *http.Request) {
	ok, cookies := sm.DeleteSession(r.Cookies())
	if !ok {
		w.WriteHeader(400)
	} else {
		http.SetCookie(w, &cookies[0])
		http.SetCookie(w, &cookies[1])
		Redirect(w, r, "/")
		return
	}
}

type State struct {
	Login   string
	LabelId uint64
	Phrase  string
}

func main() {
	config, err := ParseConfig(ConfigPath)
	if err != nil {
		panic("config parsing error: " + err.Error())
	}
	dbApi.Init(&config.PostgresConfig)
	sm.Init(&config.RedisConfig)
	defer dbApi.db.Close()

	http.Handle("/static/", http.StripPrefix("/static/", http.FileServer(http.Dir("static"))))

	http.HandleFunc("/", Main)
	http.HandleFunc("/cmdexec", ProcessCommand)
	http.HandleFunc("/create_page", CreatePage)
	http.HandleFunc("/favicon.ico", func(writer http.ResponseWriter, request *http.Request) {})
	http.HandleFunc("/phrase", PhrasePage)
	http.HandleFunc("/listing", ListingPage)
	http.HandleFunc("/login", Login)
	http.HandleFunc("/logout", Logout)
	http.HandleFunc("/register", Register)
	http.HandleFunc("/register_page", RegisterPage)
	http.HandleFunc("/labels/", ViewLabel)

	fmt.Println("Start server")
	log.Fatal(http.ListenAndServe(fmt.Sprintf("localhost:%d", config.Port), nil))
}
