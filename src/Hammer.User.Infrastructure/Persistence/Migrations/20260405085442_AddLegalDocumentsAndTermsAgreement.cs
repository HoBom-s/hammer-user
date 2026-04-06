using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hammer.User.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLegalDocumentsAndTermsAgreement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "agreed_terms_version",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "legal_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<short>(type: "smallint", nullable: false),
                    version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    effective_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_legal_documents", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_legal_documents_type_version",
                table: "legal_documents",
                columns: new[] { "type", "version" },
                unique: true);

            SeedInitialLegalDocuments(migrationBuilder);
        }

        private static void SeedInitialLegalDocuments(MigrationBuilder migrationBuilder)
        {
            var now = DateTimeOffset.UtcNow;
            var effectiveDate = new DateTimeOffset(2026, 4, 5, 0, 0, 0, TimeSpan.FromHours(9));

            // TermsOfService = 1
            migrationBuilder.InsertData(
                table: "legal_documents",
                columns: ["id", "type", "version", "effective_date", "content", "created_at", "updated_at"],
                values: new object[] { Guid.NewGuid(), (short)1, "1.0", effectiveDate, TermsOfServiceContent, now, now });

            // PrivacyPolicy = 2
            migrationBuilder.InsertData(
                table: "legal_documents",
                columns: ["id", "type", "version", "effective_date", "content", "created_at", "updated_at"],
                values: new object[] { Guid.NewGuid(), (short)2, "1.0", effectiveDate, PrivacyPolicyContent, now, now });
        }

        private const string TermsOfServiceContent = """
            # 서비스 이용약관

            **시행일:** 2026년 4월 5일
            **버전:** 1.0

            ## 제1조 (목적)

            본 약관은 해머(이하 "서비스")가 제공하는 공매 정보 서비스의 이용과 관련하여 서비스와 이용자 간의 권리, 의무 및 책임사항을 규정함을 목적으로 합니다.

            ## 제2조 (정의)

            1. "서비스"란 해머가 제공하는 공매 정보 조회, 알림, 관심 물건 관리 등 일체의 서비스를 의미합니다.
            2. "이용자"란 본 약관에 따라 서비스를 이용하는 자를 말합니다.
            3. "회원"이란 서비스에 회원가입을 한 이용자를 말합니다.

            ## 제3조 (약관의 효력 및 변경)

            1. 본 약관은 서비스 내에 공지함으로써 효력이 발생합니다.
            2. 서비스는 관련 법령에 위배되지 않는 범위에서 약관을 변경할 수 있으며, 변경 시 시행일 7일 전부터 서비스 내 공지합니다.

            ## 제4조 (회원가입 및 탈퇴)

            1. 이용자는 서비스가 정한 양식에 따라 회원가입을 신청하며, 서비스는 이를 승낙합니다.
            2. 회원은 언제든지 탈퇴를 요청할 수 있으며, 서비스는 즉시 처리합니다.
            3. 탈퇴 시 회원의 개인정보는 관련 법령에 따라 보관 후 파기합니다.

            ## 제5조 (서비스의 제공 및 변경)

            1. 서비스는 다음의 기능을 제공합니다:
               - 공매 물건 정보 조회
               - 관심 물건 알림
               - 물건 상세 정보 조회
               - 부동산 실거래가 정보 조회
            2. 서비스는 운영상, 기술상의 필요에 따라 서비스 내용을 변경할 수 있습니다.

            ## 제6조 (데이터 출처)

            본 서비스는 다음의 공공데이터를 활용하여 정보를 제공합니다:

            | 데이터 | 제공기관 | 출처 |
            |--------|----------|------|
            | 캠코(KAMCO) 공매 물건 정보 | 한국자산관리공사 | [공공데이터포털](https://www.data.go.kr/data/15000851/openapi.do) |
            | 기관공매 물건 정보 | 한국자산관리공사 | [공공데이터포털](https://www.data.go.kr/data/15000849/openapi.do) |
            | 온비드 코드 정보 | 한국자산관리공사 | [공공데이터포털](https://www.data.go.kr/data/15000920/openapi.do) |
            | 부동산 실거래가 정보 | 국토교통부 | [공공데이터포털](https://apis.data.go.kr/1613000) |

            - 상기 데이터는 공공데이터포털(data.go.kr)에서 제공하는 오픈 API를 통해 수집됩니다.
            - 서비스는 데이터의 정확성을 보장하지 않으며, 원본 데이터의 오류 또는 지연으로 인한 차이가 발생할 수 있습니다.
            - 투자 판단의 최종 책임은 이용자에게 있으며, 서비스는 이에 대한 법적 책임을 지지 않습니다.

            ## 제7조 (이용자의 의무)

            1. 이용자는 서비스를 통해 얻은 정보를 상업적 목적으로 무단 복제, 배포, 전송할 수 없습니다.
            2. 이용자는 서비스의 정상적인 운영을 방해하는 행위를 해서는 안 됩니다.
            3. 이용자는 타인의 개인정보를 침해하거나 부정하게 사용해서는 안 됩니다.

            ## 제8조 (면책조항)

            1. 서비스는 천재지변, 전쟁, 기간통신사업자의 서비스 중지 등 불가항력으로 인한 서비스 중단에 대해 책임을 지지 않습니다.
            2. 서비스는 공공데이터 제공기관의 시스템 장애, 데이터 오류 등으로 인한 정보 부정확에 대해 책임을 지지 않습니다.
            3. 이용자가 서비스를 통해 얻은 정보에 기반한 투자 결정에 대해 서비스는 책임을 지지 않습니다.

            ## 제9조 (분쟁 해결)

            본 약관과 관련하여 분쟁이 발생한 경우, 관련 법령에 따라 해결합니다.
            """;

        private const string PrivacyPolicyContent = """
            # 개인정보처리방침

            **시행일:** 2026년 4월 5일
            **버전:** 1.0

            해머(이하 "서비스")는 개인정보보호법에 따라 이용자의 개인정보를 보호하고 이와 관련한 고충을 신속하고 원활하게 처리하기 위하여 다음과 같이 개인정보처리방침을 수립·공개합니다.

            ## 제1조 (수집하는 개인정보 항목)

            서비스는 회원가입 및 서비스 제공을 위해 다음의 개인정보를 수집합니다:

            ### 필수 항목
            - **닉네임**: 서비스 내 표시명
            - **디바이스 정보**: 푸시 알림 발송을 위한 기기 식별자, 플랫폼 정보, 푸시 토큰

            ### 선택 항목
            - **이메일 주소**: 이메일/비밀번호 방식 회원가입 시 수집
            - **비밀번호**: 이메일/비밀번호 방식 회원가입 시 수집 (암호화 저장)

            ### 소셜 로그인 시 수집 항목
            - **OAuth 제공자 식별자**: Google, Apple, Kakao, Naver 계정 연동 시 제공자 고유 ID
            - **이메일 주소**: 소셜 로그인 제공자가 제공하는 경우에 한함

            ## 제2조 (개인정보의 수집 및 이용 목적)

            수집한 개인정보는 다음의 목적으로 이용합니다:

            1. **회원 관리**: 회원 식별, 로그인 인증, 계정 관리
            2. **서비스 제공**: 공매 정보 조회, 관심 물건 알림 발송
            3. **서비스 개선**: 서비스 이용 통계 분석, 기능 개선

            ## 제3조 (개인정보의 보유 및 이용 기간)

            1. 회원 탈퇴 시 개인정보는 탈퇴일로부터 7일간 보관 후 자동으로 영구 삭제됩니다. 단, 관련 법령에 따라 보존이 필요한 경우 해당 기간 동안 보관합니다.
            2. 관련 법령에 따른 보관 기간:
               - 계약 또는 청약철회 등에 관한 기록: 5년 (전자상거래법)
               - 로그인 기록: 3개월 (통신비밀보호법)

            ## 제4조 (개인정보의 제3자 제공)

            서비스는 이용자의 개인정보를 제3자에게 제공하지 않습니다. 단, 다음의 경우는 예외로 합니다:

            1. 이용자가 사전에 동의한 경우
            2. 법령에 의해 요구되는 경우

            ## 제5조 (개인정보의 처리 위탁)

            서비스는 원활한 서비스 제공을 위해 다음과 같이 개인정보를 위탁합니다:

            | 수탁업체 | 위탁 업무 |
            |----------|-----------|
            | Expo | 푸시 알림 발송 |

            ## 제6조 (개인정보의 안전성 확보 조치)

            서비스는 개인정보의 안전성 확보를 위해 다음의 조치를 취하고 있습니다:

            1. **비밀번호 암호화**: 비밀번호는 단방향 해시 함수를 사용하여 암호화 저장
            2. **전송 구간 암호화**: 모든 통신은 HTTPS(TLS)를 통해 암호화
            3. **인증 토큰 보안**: Refresh Token은 HttpOnly, Secure, SameSite=Strict 쿠키로 관리
            4. **접근 권한 관리**: 개인정보에 대한 접근 권한을 최소화

            ## 제7조 (이용자의 권리)

            이용자는 언제든지 다음의 권리를 행사할 수 있습니다:

            1. 개인정보 조회
            2. 개인정보 수정
            3. 회원 탈퇴를 통한 개인정보 삭제

            ## 제8조 (개인정보처리방침의 변경)

            본 방침은 시행일로부터 적용되며, 변경 시 서비스 내 공지합니다.
            """;

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "legal_documents");

            migrationBuilder.DropColumn(
                name: "agreed_terms_version",
                table: "users");
        }
    }
}
