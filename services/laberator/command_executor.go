package main

import (
	"bytes"
	"encoding/json"
	"errors"
	"fmt"
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

type CommandExecutor struct {
	dbApi *DBApi
}

var Commands = map[string]interface{} {
	"validate": func(ex *CommandExecutor, data []byte) ([]byte, error) {
		var pairData PairData
		err := json.Unmarshal(data, &pairData)
		if err != nil {
			return nil, errors.New("Executing error: " + err.Error())
		}
		result := ex.dbApi.Validate(&pairData.Login, &pairData.Password)
		return []byte(strconv.FormatBool(result)), nil
	},
	"check-existence": func(ex *CommandExecutor, data []byte) ([]byte, error) {
		var loginData LoginData
		err := json.Unmarshal(data, &loginData)
		if err != nil {
			return nil, errors.New("Executing error: " + err.Error() + fmt.Sprintf(" data=(%v), loginData=(%v)", string(data), loginData))
		}
		return []byte(strconv.FormatBool(ex.dbApi.IsUserExist(&loginData.Login))), nil
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
