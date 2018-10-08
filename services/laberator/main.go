package main

import (
	"fmt"
	"github.com/gorilla/websocket"
	"html/template"
	"io"
	"io/ioutil"
	"log"
	"net/http"
	"strconv"
)

var dbApi DBApi
var sm SessionManager

func Redirect(w http.ResponseWriter, r *http.Request, url string) {
	fmt.Fprintf(w, `<html><head></head><body><script>window.location.replace("%v")</script></body></html>`, url)
}

func Register(w http.ResponseWriter, r *http.Request) {
	r.ParseForm()
	login := r.Form.Get("login")
	password := r.Form.Get("password")
	if len(login) == 0 || len(password) == 0 {
		w.WriteHeader(400)
	} else if dbApi.IsUserExist(login) {
		w.WriteHeader(400)
	} else {
		dbApi.Register(&login, &password)
		cookies := sm.CreateSession(login)
		for _, cookie := range cookies {
			http.SetCookie(w, &cookie)
		}
		Redirect(w, r, "/main")
	}
}

func Login(w http.ResponseWriter, r *http.Request) {
	r.ParseForm()
	login := r.Form.Get("login")
	password := r.Form.Get("password")
	if len(login) == 0 || len(password) == 0 {
		w.WriteHeader(400)
	} else if !dbApi.Validate(&login, &password) {
		w.WriteHeader(400)
	} else {
		cookies := sm.CreateSession(login)
		for _, cookie := range cookies {
			http.SetCookie(w, &cookie)
		}
		Redirect(w, r, "/main")
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
	Exec(w, "templates/main.html", &State{Login:logged})
}

func RegisterPage(w http.ResponseWriter, r *http.Request) {
	ok, _ := sm.ValidateSession(r.Cookies())
	if ok {
		Redirect(w, r, "/main")
	} else {
		Exec(w, "templates/register.html", &State{})
	}
}

var upgrader = websocket.Upgrader{} // use default options

func IsRegistered(w http.ResponseWriter, r *http.Request) {
	c, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		log.Print("upgrade:", err)
		return
	}
	defer c.Close()
	for {
		mt, login, err := c.ReadMessage()
		if err != nil {
			log.Println("read error:", err)
			break
		}
		isRegistered := dbApi.IsUserExist(string(login))
		err = c.WriteMessage(mt, []byte(strconv.FormatBool(isRegistered)))
		if err != nil {
			log.Println("write:", err)
			break
		}
	}
}

func Ws(w http.ResponseWriter, r *http.Request) {
	t := template.New("")
	t.ParseFiles("templates/ws.html")
	t.ExecuteTemplate(w, "url", "ws://" + r.Host + "/echo")
}

func Logout(w http.ResponseWriter, r *http.Request) {
	ok, cookies := sm.DeleteSession(r.Cookies())
	if !ok {
		w.WriteHeader(400)
	} else {
		http.SetCookie(w, &cookies[0])
		http.SetCookie(w, &cookies[1])
		Redirect(w, r, "/main")
	}
}

type State struct {
	Login string
}

func main() {
	dbApi.Init()
	sm.Init()
	defer dbApi.db.Close()

	http.Handle("/static/", http.StripPrefix("/static/", http.FileServer(http.Dir("static"))))

	http.HandleFunc("/register", Register)
	http.HandleFunc("/login", Login)
	http.HandleFunc("/main", Main)
	http.HandleFunc("/ws", Ws)
	http.HandleFunc("/isreg", IsRegistered)
	http.HandleFunc("/logout", Logout)
	http.HandleFunc("/register_page", RegisterPage)

	log.Fatal(http.ListenAndServe(":8080", nil))
}
