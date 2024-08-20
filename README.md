#Perfume Tracker: 
A simple app to track my perfume collection and try out NextJS

##Docker commands:
docker build -t fuzzydice555/perfume-tracker .
docker run -p 3000:3000 fuzzydice555/perfume-tracker
docker push fuzzydice555/perfume-tracker   

##Docker compose:
services:
    perfume-tracker:
        image: fuzzydice555/perfume-tracker
        ports:
          - 3000:3000
        environment:
          - DATABASE_URL=postgresql://user:pass@dbserver:port/database
          - R2_API_ADDRESS="http://address:port"
        restart: unless-stopped

#Image upload:
Via separate Go microservice: https://github.com/fuzzydice555/r2-api
Configured in env: R2_API_ADDRESS

##Docker compose:
services:
    perfume-tracker:
        image: fuzzydice555/r2-api-go
        ports:
          - 8088:8080
        environment:
          - R2_ENDPOINT=...
          - R2_BUCKET=test
          - R2_REGION=auto
          - R2_ACCESS_KEY=...
          - R2_SECRET_KEY=...
          - R2_UPLOAD_EXPIRY_MINUTES=30
          - R2_DOWNLOAD_EXPIRY_MINUTES=30
        restart: unless-stopped