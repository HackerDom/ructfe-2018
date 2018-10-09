package main

import (
	"bytes"
	"encoding/json"
	"errors"
	"fmt"
	"net/http"
	"strconv"
)

const VALIDATE_PAIR_CMD string = "validate"
const CHECK_EXISTENCE_CMD string = "check-existence"

type PairData struct {
	Login string
	Password string
}

type LoginData struct {
	Login string
}

type CreatingData struct {
	Text string
	Font string
	Size uint
	RawCookies string
}

type CommandExecutor struct {
	dbApi *DBApi
	sm *SessionManager
}

func parseCookies(data string) []*http.Cookie {
	header := http.Header{}
	header.Add("Cookie", data)
	req := http.Request{Header: header}
	return req.Cookies()
}

var Commands = map[string]interface{} {
	"validate": func(ex *CommandExecutor, data []byte) ([]byte, error) {
		var pairData PairData
		err := json.Unmarshal(data, &pairData)
		if err != nil {
			return nil, errors.New("unmarshalling error: " + err.Error() + fmt.Sprintf(" data=(%v)", string(data)))
		}
		result := ex.dbApi.Validate(&pairData.Login, &pairData.Password)
		return []byte(strconv.FormatBool(result)), nil
	},
	"check-existence": func(ex *CommandExecutor, data []byte) ([]byte, error) {
		var loginData LoginData
		err := json.Unmarshal(data, &loginData)
		if err != nil {
			return nil, errors.New("unmarshalling error: " + err.Error() + fmt.Sprintf(" data=(%v)", string(data)))
		}
		return []byte(strconv.FormatBool(ex.dbApi.IsUserExist(&loginData.Login))), nil
	},
	"create": func(ex *CommandExecutor, data []byte) ([]byte, error) {
		var creatingData CreatingData
		err := json.Unmarshal(data, &creatingData)
		if err != nil {
			return nil, errors.New("unmarshalling error: " + err.Error() + fmt.Sprintf(" data=(%v)", string(data)))
		}
		cookies := parseCookies(creatingData.RawCookies)
		ok, login := ex.sm.ValidateSession(cookies)
		if ok {
			ex.dbApi.CreateLabel(creatingData.Text, creatingData.Font, creatingData.Size, login)
		}
		return []byte(strconv.FormatBool(ok)), nil
	},
}

func (ex *CommandExecutor) Execute(data []byte) ([]byte, error) {
	for command, execFunc := range Commands {
		if bytes.HasPrefix(data, []byte(command)) {
			return execFunc.(func(ex *CommandExecutor, data []byte) ([]byte, error))(ex, data[len(command):])
		}
	}
	return nil, errors.New("unknown command")
}
