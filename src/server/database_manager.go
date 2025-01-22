package server

import (
	"database/sql"
	"fmt"
	"log"
	"os"
)

type DataBaseManager struct {
	username string
	password string
}

var defaultUsername = ""
var defaultPassword = ""

func NewDataBaseManager(username, password string) *DataBaseManager {
	return &DataBaseManager{username: defaultUsername, password: defaultPassword}
}

func (db *DataBaseManager) SetDataBase(operation, value string, count int) {
	switch operation {
	case "insert":
		db.InsertData("databaseName", "tableName", "columnName", "varchar(100)", value)
	case "delete":
		db.DeleteData("databaseName", "tableName", "columnName", count)
	case "update":
		db.UpdateData("databaseName", "tableName", "columnName", count, value)
	case "select":
		var rowsData map[int]string
		var columnsData map[string]string
		var singleData string
		db.SelectData("databaseName", "tableName", &rowsData, &columnsData, &singleData, "columnName", count)
		PrintLog("查找到的数据是：" + singleData)
	case "close":
		os.Exit(0)
	}
}

func (db *DataBaseManager) InsertData(databaseName, tableName, columnName, columnType, value string) {
	if !db.JudgeIsExists(databaseName, tableName, columnName) {
		db.CreateDatabase(databaseName)
	}

	if !db.JudgeTableIsExists(databaseName, tableName) {
		db.CreateTable(databaseName, tableName)
	}

	connStr := fmt.Sprintf("%s:%s@tcp(localhost:3306)/%s?charset=utf8&parseTime=True", db.username, db.password, databaseName)
	dbConn, err := sql.Open("mysql", connStr)
	if err != nil {
		log.Fatal(err)
	}
	defer dbConn.Close()

	if !db.JudgeColumnIsExists(databaseName, tableName, columnName) {
		_, err := dbConn.Exec(fmt.Sprintf("ALTER TABLE %s ADD %s %s", tableName, columnName, columnType))
		if err != nil {
			log.Fatal(err)
		}
		PrintLog("成功创建新列:" + columnName)
	}

	_, err = dbConn.Exec(fmt.Sprintf("INSERT INTO %s (%s) VALUES ('%s')", tableName, columnName, value))
	if err != nil {
		log.Fatal(err)
	}
	PrintLog("成功插入数据:" + value)
}

func (db *DataBaseManager) DeleteData(databaseName, tableName, columnName string, primaryKeyValue int) {
	if !db.JudgeColumnIsExists(databaseName, tableName, columnName) {
		return
	}

	connStr := fmt.Sprintf("%s:%s@tcp(localhost:3306)/%s?charset=utf8&parseTime=True", db.username, db.password, databaseName)
	dbConn, err := sql.Open("mysql", connStr)
	if err != nil {
		log.Fatal(err)
	}
	defer dbConn.Close()

	var commandString string
	if columnName == "" && primaryKeyValue != -1 {
		commandString = fmt.Sprintf("DELETE FROM %s WHERE id = ?", tableName)
	} else if columnName == "" {
		commandString = fmt.Sprintf("ALTER TABLE %s DROP COLUMN %s", tableName, columnName)
	} else if primaryKeyValue != -1 {
		commandString = fmt.Sprintf("DELETE FROM %s WHERE id = ?", tableName)
	}

	if commandString != "" {
		_, err := dbConn.Exec(commandString, primaryKeyValue)
		if err != nil {
			log.Fatal("删除失败:", err)
		}
		PrintLog("删除成功")
	}
}

func (db *DataBaseManager) UpdateData(databaseName, tableName, columnName string, primaryKeyValue int, value string) {
	if !db.JudgeColumnIsExists(databaseName, tableName, columnName) {
		return
	}

	connStr := fmt.Sprintf("%s:%s@tcp(localhost:3306)/%s?charset=utf8&parseTime=True", db.username, db.password, databaseName)
	dbConn, err := sql.Open("mysql", connStr)
	if err != nil {
		log.Fatal(err)
	}
	defer dbConn.Close()

	_, err = dbConn.Exec(fmt.Sprintf("UPDATE %s SET %s = ? WHERE id = ?", tableName, columnName), value, primaryKeyValue)
	if err != nil {
		log.Fatal(err)
	}
	fmt.Printf("修改 %s 列的 %d 行为 %s\n", columnName, primaryKeyValue, value)
}

func (db *DataBaseManager) SelectData(databaseName, tableName string, rowsData *map[int]string, columnsData *map[string]string, singleData *string, columnName string, primaryKeyValue int) {
	if !db.JudgeColumnIsExists(databaseName, tableName, columnName) {
		return
	}

	connStr := fmt.Sprintf("%s:%s@tcp(localhost:3306)/%s?charset=utf8&parseTime=True", db.username, db.password, databaseName)
	dbConn, err := sql.Open("mysql", connStr)
	if err != nil {
		log.Fatal(err)
	}
	defer dbConn.Close()

	var query string
	if columnName == "" && primaryKeyValue != -1 {
		query = fmt.Sprintf("SELECT %s FROM %s WHERE id = ?", columnName, tableName)
	} else if columnName == "" {
		query = fmt.Sprintf("SELECT id, %s FROM %s", columnName, tableName)
	} else if primaryKeyValue != -1 {
		query = fmt.Sprintf("SELECT * FROM %s WHERE id = ?", tableName)
	}

	rows, err := dbConn.Query(query, primaryKeyValue)
	if err != nil {
		log.Fatal(err)
	}
	defer rows.Close()

	if columnName != "" && primaryKeyValue == -1 {
		for rows.Next() {
			var id int
			var value string
			if err := rows.Scan(&id, &value); err != nil {
				log.Fatal(err)
			}
			(*rowsData)[id] = value
		}
	} else if primaryKeyValue != -1 {
		for rows.Next() {
			columns, err := rows.Columns()
			if err != nil {
				log.Fatal(err)
			}

			values := make([]interface{}, len(columns))
			for i := range values {
				values[i] = new(string)
			}

			if err := rows.Scan(values...); err != nil {
				log.Fatal(err)
			}

			for i, column := range columns {
				(*columnsData)[column] = *(values[i].(*string))
			}
		}
	} else if primaryKeyValue == -1 {
		for rows.Next() {
			var value string
			if err := rows.Scan(&value); err != nil {
				log.Fatal(err)
			}
			*singleData = value
		}
	}
}

func (db *DataBaseManager) CreateDatabase(databaseName string) {
	connStr := fmt.Sprintf("%s:%s@tcp(localhost:3306)/?charset=utf8&parseTime=True", db.username, db.password)
	dbConn, err := sql.Open("mysql", connStr)
	if err != nil {
		log.Fatal(err)
	}
	defer dbConn.Close()

	_, err = dbConn.Exec(fmt.Sprintf("CREATE DATABASE %s", databaseName))
	if err != nil {
		log.Fatal(err)
	}
	PrintLog("数据库创建成功:" + databaseName)
}

func (db *DataBaseManager) CreateTable(databaseName, tableName string) {
	connStr := fmt.Sprintf("%s:%s@tcp(localhost:3306)/%s?charset=utf8&parseTime=True", db.username, db.password, databaseName)
	dbConn, err := sql.Open("mysql", connStr)
	if err != nil {
		log.Fatal(err)
	}
	defer dbConn.Close()

	_, err = dbConn.Exec(fmt.Sprintf("CREATE TABLE %s (id INT AUTO_INCREMENT PRIMARY KEY)", tableName))
	if err != nil {
		log.Fatal(err)
	}
	PrintLog("表创建成功:" + tableName)
}

func (db *DataBaseManager) JudgeIsExists(databaseName, tableName, columnName string) bool {
	if !db.JudgeDataBaseIsExists(databaseName) {
		return false
	}
	if tableName != "" && !db.JudgeTableIsExists(databaseName, tableName) {
		return false
	}
	if columnName != "" && !db.JudgeColumnIsExists(databaseName, tableName, columnName) {
		return false
	}
	return true
}

func (db *DataBaseManager) JudgeDataBaseIsExists(databaseName string) bool {
	connStr := fmt.Sprintf("%s:%s@tcp(localhost:3306)/?charset=utf8&parseTime=True", db.username, db.password)
	dbConn, err := sql.Open("mysql", connStr)
	if err != nil {
		log.Fatal(err)
	}
	defer dbConn.Close()

	var exists string
	err = dbConn.QueryRow(fmt.Sprintf("SELECT SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = '%s'", databaseName)).Scan(&exists)
	if err != nil && err.Error() != "sql: no rows in result set" {
		log.Fatal(err)
	}
	return exists != ""
}

func (db *DataBaseManager) JudgeTableIsExists(databaseName, tableName string) bool {
	connStr := fmt.Sprintf("%s:%s@tcp(localhost:3306)/%s?charset=utf8&parseTime=True", db.username, db.password, databaseName)
	dbConn, err := sql.Open("mysql", connStr)
	if err != nil {
		log.Fatal(err)
	}
	defer dbConn.Close()

	var exists string
	err = dbConn.QueryRow(fmt.Sprintf("SHOW TABLES LIKE '%s'", tableName)).Scan(&exists)
	if err != nil && err.Error() != "sql: no rows in result set" {
		log.Fatal(err)
	}
	return exists != ""
}

func (db *DataBaseManager) JudgeColumnIsExists(databaseName, tableName, columnName string) bool {
	connStr := fmt.Sprintf("%s:%s@tcp(localhost:3306)/%s?charset=utf8&parseTime=True", db.username, db.password, databaseName)
	dbConn, err := sql.Open("mysql", connStr)
	if err != nil {
		log.Fatal(err)
	}
	defer dbConn.Close()

	var exists string
	err = dbConn.QueryRow(fmt.Sprintf("SHOW COLUMNS FROM %s LIKE '%s'", tableName, columnName)).Scan(&exists)
	if err != nil && err.Error() != "sql: no rows in result set" {
		log.Fatal(err)
	}
	return exists != ""
}