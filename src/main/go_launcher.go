package main

import (
	"os"
	"os/signal"
	"my_project/server"
	"syscall"
)

func main() {
	server.InitLog()

	// 启动服务器
	serverManager := server.GetServerManager()
	serverManager.Start()

	// 捕获退出信号
	c := make(chan os.Signal, 1)
	signal.Notify(c, syscall.SIGINT, syscall.SIGTERM)
	<-c

	// 停止服务器
	serverManager.Stop()
}