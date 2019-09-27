# dotnetcore-nats-play
NATS Playground

In the solution folder run the following command
```
docker-compose -f .\docker-compose.yml up --scale nats-replier=2 --scale nats-subscribe=2  
```
To verify that it is up navigate [http://localhost:8222/](http://localhost:8222/)  

nats-replier is listening to the "dog" subject and is in a queue group, so the replier will be randomly selected to process the message.

```
...src\Requestor>dotnet run -verbose -subject=dog -count 50
```

## NATS Streaming Server
I have 2 intances of stan-sub running, where each has a different clientID.  They shared duties of any message published to their queue.

```
...src\stan-pub>dotnet run -server nats://localhost:4223 -subject dog -count 10 -verbose
```




The examples, which I copied from [nats.net](https://github.com/nats-io/nats.net) assume the standard ports (4222) which you can override.  

