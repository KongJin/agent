## Encoding Guide (한글 깨짐 방지)

- 모든 소스 파일은 UTF-8로 저장합니다(BOM 여부 무관).
- PowerShell로 파일을 저장/갱신할 때는 반드시 `-Encoding UTF8`을 지정합니다.
  - 예시: `Get-Content file.cs | Set-Content file.cs -Encoding UTF8`
  - 예시: `"...text..." | Out-File file.txt -Encoding UTF8`
- 기존 파일을 다시 쓸 때 기본 인코딩(CP949/ANSI 등)으로 덮어쓰지 않도록, 항상 인코딩을 명시합니다.
- 한글이 포함된 문자열 리터럴은 가급적 verbatim 리터럴(`@""`)을 사용해 불필요한 이스케이프를 줄입니다.
- 다른 도구/스크립트가 파일을 건드릴 때도 UTF-8을 유지하는지 확인합니다.
