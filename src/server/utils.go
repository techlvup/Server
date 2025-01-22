package server

import (
	"strings"
	"os"
	"fmt"
)

// 分割消息的辅助函数
func splitMessage(message string) []string {
	// 这里假设消息以 | 分隔
	return strings.Split(message, "|")
}

func InitLog() {
	logFilePath := "log.txt"
	
	contentByte := []byte("")

	//文件不存在会创建，存在则覆盖
	os.WriteFile(logFilePath, contentByte, 0644)
}

func PrintLog(currs ...any) {
	logFilePath := "log.txt"

	data, err := os.ReadFile(logFilePath)

	if err != nil {
		return
	}

	curr := ""

	if len(currs) > 0 {
		for _, des := range currs {
			curr += fmt.Sprintln(des)
		}
	}

	pre := string(data)
	contentStr := ""

	if pre == "" {
		contentStr = curr
	}else
	{
		contentStr = pre + "\n\n\n" + curr
	}

	contentByte := []byte(contentStr)

	//文件不存在会创建，存在则覆盖
	os.WriteFile(logFilePath, contentByte, 0644)

	fmt.Println(curr)
}