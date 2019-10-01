
$env:NatsPodName = $(kubectl get pods --namespace default -l "app=nats" -o jsonpath="{.items[0].metadata.name}")
Write-Output $env:NatsPodName

$env:NatsUsers=$(kubectl get cm --namespace default dark-watch-nats -o jsonpath='{.data.*}')| Select-String -Pattern "user"
Write-Output $env:NatsUsers

$env:NatsPassword=$(kubectl get cm --namespace default dark-watch-nats -o jsonpath='{.data.*}')| Select-String -Pattern "password"
Write-Output $env:NatsPassword


kubectl port-forward --namespace default $env:NatsPodName 8222:8222 4222:4222