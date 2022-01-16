## Channel-Core
C#实现的QQ频道Bot消息处理端，用于连接QQ频道官方的WebSocket服务器，分发并管理插件消息

## 填写必需信息
- 打开`Config/Config.json`
- 填写管理端分发的`AppID` `AppKey` `Token`
```json
{
    "AppID": "10*****26",
    "Token": "XeW****************Jo",
    "AppKey": "67a*******************b0"
}
```

## 待填功能
- [x] 重连
- [x] 消息队列
- [ ] WebUI
- [ ] 日志分发
- [ ] 更多的扩展