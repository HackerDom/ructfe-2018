package main

import (
	"encoding/base64"
	"regexp"
)

var pattern, _ = regexp.Compile("^[\\w=/]{1,40}$")
var phrasePattern, _ = regexp.Compile("^[a-zA-Z0-9!@#$%&*()_+=/., ]{1,100}$")

func IsValidLogin(login *string) bool {
	return pattern.Match([]byte(*login))
}

func IsValidPassword(password *string) bool {
	data, err := base64.StdEncoding.DecodeString(*password)
	return err == nil && len(data) >= 1 && len(data) < 40
}

func IsValidPhrase(phrase *string) bool {
	data, err := base64.StdEncoding.DecodeString(*phrase)
	return err == nil && phrasePattern.Match(data)
}
