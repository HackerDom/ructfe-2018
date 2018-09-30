package main

import (
	"crypto/sha512"
	"encoding/base64"
	"github.com/go-redis/redis"
	"math/rand"
	"net/http"
	"time"
)

const CookieExpirationTime = time.Hour

type SessionManager struct {
	client *redis.Client
}

func (sm *SessionManager) Init() {
	sm.client = redis.NewClient(&redis.Options{Addr: "localhost:6379", Password: "", DB: 0})
}

func generateSessionId(login, salt string) string {
	hash := sha512.Sum512([]byte(login + salt))
	return base64.StdEncoding.EncodeToString(hash[:])
}

var alphabet = []rune("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890")

func generateRandomSalt() string {
	b := make([]rune, 10)
	for i := range b {
		b[i] = alphabet[rand.Intn(len(alphabet))]
	}
	return string(b)
}

func (sm *SessionManager) CreateSession(login string) []http.Cookie {
	salt := generateRandomSalt()
	sid := generateSessionId(login, salt)
	if sm.client == nil {
		panic("Client is a nil!")
	}
	sm.client.Set(login, salt, CookieExpirationTime)
	cookies := make([]http.Cookie, 2)
	cookies[0] = http.Cookie{Name: "login", Value: login, Expires: time.Now().Add(CookieExpirationTime)}
	cookies[1] = http.Cookie{Name: "sid", Value: sid, Expires: time.Now().Add(CookieExpirationTime)}
	return cookies
}

func (sm *SessionManager) ValidateSession(cookies []*http.Cookie) (bool, string) {
	if len(cookies) != 2 {
		return false, ""
	}
	var login, sid string
	for _, cookie := range cookies {
		switch cookie.Name {
		case "login":
			login = cookie.Value
		case "sid":
			sid = cookie.Value
		}
	}
	salt := sm.client.Get(login).Val()
	return generateSessionId(login, salt) == sid, login
}

func (sm *SessionManager) DeleteSession(cookies []*http.Cookie) (bool, []http.Cookie) {
	ok, login := sm.ValidateSession(cookies)
	if !ok {
		return false, []http.Cookie{}
	} else {
		sm.client.Del(login)
		cookies := make([]http.Cookie, 2)
		cookies[0] = http.Cookie{Name: "login", Value: "", Expires: time.Now()}
		cookies[1] = http.Cookie{Name: "sid", Value: "", Expires: time.Now()}
		return true, cookies
	}
}
