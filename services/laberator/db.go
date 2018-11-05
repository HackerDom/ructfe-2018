package main

import (
	"bytes"
	"errors"
	"fmt"
	"github.com/jinzhu/gorm"
	_ "github.com/jinzhu/gorm/dialects/postgres"
	"github.com/werelaxe/fast-hash"
)

const ListingLimit = 10

type User struct {
	ID           uint `gorm:"primary_key"`
	Login        string
	PasswordHash []byte
}

type DBApi struct {
	db *gorm.DB
}

type DBApiError string

type Label struct {
	ID    uint `gorm:"primary_key"`
	Text  string
	Font  string
	Size  uint
	Owner string
}

type PostgresConfig struct {
	DBName   string
	Host     string
	Port     uint
	User     string
	Password string
}

func (err DBApiError) Error() string {
	return fmt.Sprintf("Database api error: %v", string(err))
}

func (api *DBApi) IsUserExist(login *string) bool {
	var users []User
	api.db.Where("Login = ?", *login).Find(&users)
	return len(users) != 0
}

func (api *DBApi) Register(login, password *string) error {
	var users []User
	api.db.Where("Login = ?", login).Find(&users)
	if len(users) != 0 {
		return DBApiError(fmt.Sprintf("User with login '%s' is already exist", *login))
	}
	passwordHash := hasher.GetHash(([]byte)(*password))
	user := User{Login: *login, PasswordHash: passwordHash[:]}
	api.db.Create(&user)
	return nil
}

func (api *DBApi) Validate(login, password *string) bool {
	var users []User
	api.db.Where("Login = ?", login).Find(&users)
	if len(users) != 1 {
		return false
	}
	passwordHash := hasher.GetHash([]byte(*password))
	if bytes.Equal(users[0].PasswordHash, passwordHash[:]) {
		return true
	}
	return false
}

func (api *DBApi) CreateLabel(text, font string, size uint, owner string) {
	label := Label{Text: text, Font: font, Size: size, Owner: owner}
	api.db.Create(&label)
}

func (api *DBApi) Listing(offset uint, owner string) *[]Label {
	var labels []Label
	api.db.Where("owner = ?", owner).Offset(offset).Limit(ListingLimit).Find(&labels)
	return &labels
}

func (api *DBApi) ViewLabel(labelId uint) (*Label, error) {
	var labels []Label
	api.db.Where("id = ?", labelId).Find(&labels)
	if len(labels) != 1 {
		return nil, errors.New(fmt.Sprintf("len(labels with id=%v) = %v", labelId, len(labels)))
	}
	return &labels[0], nil
}

func (api *DBApi) GetLastUsers() *[]User {
	var users []User
	api.db.Order("ID DESC").Limit(ListingLimit).Find(&users)
	return &users
}

func (api *DBApi) CheckLabelOwner(owner string, labelId uint64) bool {
	var labels []Label
	api.db.Where("id = ?", labelId).Find(&labels)
	if len(labels) != 1 {
		return false
	}
	return labels[0].Owner == owner
}

func (api *DBApi) Init(config *PostgresConfig) {
	var err error
	api.db, err = gorm.Open("postgres", fmt.Sprintf("host=localhost port=%d user=%s dbname=%s password=%s", config.Port, config.User, config.DBName, config.Password))
	if err != nil {
		panic(fmt.Sprintf("failed to connect database: %s", err))
	}
	api.db.AutoMigrate(&User{})
	api.db.AutoMigrate(&Label{})
}
