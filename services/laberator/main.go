package main

import (
	"fmt"
	"html/template"
	"log"
	"net/http"
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
		fmt.Fprintf(w, "empty password or login")
		w.WriteHeader(400)
	} else if dbApi.IsUserExist(login) {
		fmt.Fprintf(w, "this login is already used")
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

func Main(w http.ResponseWriter, r *http.Request) {
	ok, login := sm.ValidateSession(r.Cookies())
	if ok {
		t := template.New("tmpl")
		t.ParseFiles("templates/main.html")
		t.ExecuteTemplate(w, "login", login)
	} else {
		fmt.Fprint(w, "Not authorized!")
	}
}

func main() {
	dbApi.Init()
	sm.Init()
	defer dbApi.db.Close()

	http.HandleFunc("/register", Register)

	http.HandleFunc("/login", Login)

	http.HandleFunc("/main", Main)

	log.Fatal(http.ListenAndServe(":8080", nil))
}
