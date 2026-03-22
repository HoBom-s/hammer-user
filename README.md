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

| Service                                                         | Description           |
|-----------------------------------------------------------------|-----------------------|
| [hammer-gateway](https://github.com/HoBom-s/hammer-gateway)     | API Gateway           |
| [hammer-user](https://github.com/HoBom-s/hammer-user)           | User & Auth           |
| [hammer-auction](https://github.com/HoBom-s/hammer-auction)     | Auction API           |
| [hammer-collector](https://github.com/HoBom-s/hammer-collector) | Data Collector        |
| [hammer-support](https://github.com/HoBom-s/hammer-support)     | Logging, FCM, Support |

## Getting Started

### 환경 설정

`.env.example`을 복사해서 `.env.local`을 만들고 값을 채운다.

```bash
cp .env.example .env.local
```

### 로컬 실행

```bash
dotnet run --project src/Hammer.User.Api
```

### Docker 실행

```bash
docker build -t hammer-user .
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=live \
  -e ConnectionStrings__DefaultConnection="Host=...;Port=5432;Database=bear;Username=...;Password=...;Search Path=hammer" \
  -e Jwt__SecretKey="your-secret-key" \
  hammer-user
```

> `.env` 파일은 로컬 개발용. Docker 배포 시에는 컨테이너 환경변수로 직접 주입한다.

## Branch Strategy

- `main` — Production
- `develop` — Development (default)
