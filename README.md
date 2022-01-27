# Landlord-Bot
 
一个简单的 QQ 斗地主机器人，基于 Sora 和 OneBot 协议。

### 已经实现
- [x] 聊天接入回复机器人 API
- [x] 简单的积分系统
- [x] 每日签到 & 猜数字
- [x] 简单的斗地主功能（仅可用于单个群聊，无法处理牌型）

### 未来计划
- [ ] 斗地主接入积分系统
- [ ] 斗地主牌型判断
- [ ] 整体重构，适配多群同时游戏

## 使用方法
搭配 [go-cqhttp](https://github.com/Mrs4s/go-cqhttp) 使用，以反向 WebSocket 方式连接。
