# Landlord-Bot
 
一个简单的 QQ 斗地主机器人，基于 Sora 和 OneBot 协议。**仍在开发中，目前可能存在较多问题。**

### 已经实现
- [x] 聊天接入回复机器人 API
- [x] 简单的积分系统
- [x] 每日签到 & 猜数字
- [x] 简单的斗地主功能（无法处理牌型）
- [x] 斗地主接入积分系统
- [x] 适配多群同时游戏


### 未来计划
- [ ] 斗地主基本牌型种类判断
- [ ] 游戏规则完整处理


## 使用方法
搭配 [go-cqhttp](https://github.com/Mrs4s/go-cqhttp) 使用，以反向 WebSocket 方式连接( ws://127.0.0.1:8080 )。


## 使用的开源库
[Sora](https://github.com/DeepOceanSoft/Sora)

[Microsoft.Data.Sqlite](https://github.com/dotnet/efcore)
