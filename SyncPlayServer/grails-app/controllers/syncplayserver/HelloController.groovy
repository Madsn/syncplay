package syncplayserver
import SyncPlayServer.Client

class HelloController {

  def index(){
    render "testing"
  }

  def get() {
    def c = Client.findByName(params.name)
    render c.seconds.toString().replaceAll("\\.",",")
  }

  def update() {
    def c = Client.findByName(params.name)
    c.seconds = Double.parseDouble(params.seconds.replaceAll(",","."))
    c.save()
    render "done"
  }

  def create() {
    def c = new Client(name:params.name,
      movie:params.movie,
      seconds:Double.parseDouble(params.seconds.replaceAll(",",".")))
    c.save()
    render "created"
  }
}
