package syncplayserver
import SyncPlayServer.Client

class HelloController {

    def index() { 
        def c = new Client(name:"aasdad", movie:"dasd", seconds:12313)
        c.save()
        render Client.list() 
    }
}
