docker build .

docker compose:

services:
    perfume-tracker:
        image: fuzzydice555/perfume-tracker
        ports:
          - 3000:3000
        restart: unless-stopped