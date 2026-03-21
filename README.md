# Hammer User

Hammer 경매 플랫폼의 User & Auth Service.

## Stack

- ASP.NET (.NET 10)
- PostgreSQL
- JWT (Access / Refresh Token)

## Features

- 회원가입, 로그인
- JWT 발급 및 갱신
- 사용자 프로필 관리

## Services

| Service | Description |
|---------|-------------|
| [hammer-gateway](https://github.com/HoBom-s/hammer-gateway) | API Gateway |
| [hammer-user](https://github.com/HoBom-s/hammer-user) | User & Auth |
| [hammer-auction](https://github.com/HoBom-s/hammer-auction) | Auction API |
| [hammer-collector](https://github.com/HoBom-s/hammer-collector) | Data Collector |
| [hammer-support](https://github.com/HoBom-s/hammer-support) | Logging, FCM, Support |

## Getting Started

```bash
dotnet restore
dotnet run --project src/Hammer.User
```

## Branch Strategy

- `main` — Production
- `develop` — Development (default)
