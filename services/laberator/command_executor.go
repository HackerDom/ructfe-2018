package main

import (
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

type ListingData struct {
	Offset uint
	RawCookies string
}

type ViewData struct {
	LabelId uint
	RawCookies string
}

type CmdRequest struct {
	Command string
	Data string
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

func createUnmarshallingError(err error, data []byte) error {
	return errors.New(fmt.Sprintf("unmarshalling error: %v, data=(%v)", err.Error(), string(data)))
}

var Commands = map[string]interface{} {
	"validate": func(ex *CommandExecutor, data []byte) ([]byte, error) {
		var pairData PairData
		err := json.Unmarshal(data, &pairData)
		if err != nil {
			return nil, createUnmarshallingError(err, data)
		}
		result := ex.dbApi.Validate(&pairData.Login, &pairData.Password)
		return []byte(strconv.FormatBool(result)), nil
	},
	"check-existence": func(ex *CommandExecutor, data []byte) ([]byte, error) {
		var loginData LoginData
		err := json.Unmarshal(data, &loginData)
		if err != nil {
			return nil, createUnmarshallingError(err, data)
		}
		return []byte(strconv.FormatBool(ex.dbApi.IsUserExist(&loginData.Login))), nil
	},
	"create": func(ex *CommandExecutor, data []byte) ([]byte, error) {
		var creatingData CreatingData
		err := json.Unmarshal(data, &creatingData)
		if err != nil {
			return nil, createUnmarshallingError(err, data)
		}
		cookies := parseCookies(creatingData.RawCookies)
		ok, login := ex.sm.ValidateSession(cookies)
		if ok {
			ex.dbApi.CreateLabel(creatingData.Text, creatingData.Font, creatingData.Size, login)
		}
		return []byte(strconv.FormatBool(ok)), nil
	},
	"list": func(ex *CommandExecutor, data []byte) ([]byte, error) {
		var listingData ListingData
		err := json.Unmarshal(data, &listingData)
		if err != nil {
			return nil, createUnmarshallingError(err, data)
		}
		cookies := parseCookies(listingData.RawCookies)
		ok, login := ex.sm.ValidateSession(cookies)
		if ok {
			labels := ex.dbApi.Listing(listingData.Offset, login)
			rawResponse, err := json.Marshal(labels)
			if err != nil {
				return nil, errors.New(fmt.Sprintf("marshalling error: %v, data=(%v)", err.Error(), labels))
			}
			return rawResponse, nil
		}
		return []byte("false"), nil
	},
	"view": func(ex *CommandExecutor, data []byte) ([]byte, error) {
		var viewData ViewData
		err := json.Unmarshal(data, &viewData)
		if err != nil {
			return nil, createUnmarshallingError(err, data)
		}
		cookies := parseCookies(viewData.RawCookies)
		ok, _ := ex.sm.ValidateSession(cookies)
		if !ok {
			return nil, errors.New("invalid session")
		}
		label, err := ex.dbApi.ViewLabel(viewData.LabelId)
		if err != nil {
			return nil, errors.New(fmt.Sprintf("db request error: %v, labelId=(%v)", err.Error(), viewData.LabelId))
		}
		rawLabel, err := json.Marshal(*label)
		if err != nil {
			return nil, errors.New(fmt.Sprintf("marshalling error: %v, label=(%v)", err.Error(), *label))
		}
		return rawLabel, nil
	},
}

func (ex *CommandExecutor) Execute(data []byte) ([]byte, error) {
	var cmdRequest CmdRequest
	err := json.Unmarshal(data, &cmdRequest)
	if err != nil {
		return nil, createUnmarshallingError(err, data)
	}
	execFunc := Commands[cmdRequest.Command]
	if execFunc != nil {
		return execFunc.(func(ex *CommandExecutor, data []byte) ([]byte, error))(ex, []byte(cmdRequest.Data))
	}
	return nil, errors.New(fmt.Sprintf("unknown command: %s", cmdRequest.Command))
}
