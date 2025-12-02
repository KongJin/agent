# C# 문자열 이스케이프 가이드

## 문제 상황
Agent.cs에서 JSON 형식의 예시를 문자열로 표현할 때 발생한 문자열 이스케이프 오류

## 원인 분석

### ❌ 실패한 패턴들

#### 1. `$@"..."` 문자열에 중괄호 사용 (보간 충돌)
```csharp
// 이렇게 하면 안됨 - 중괄호가 보간으로 인식됨
string prompt = $@"
도구 호출: {"tool":"ToolName"}
";
// 오류: CS1002, CS1010 등 - 보간 문자열로 인식됨
```

#### 2. 중괄호를 이스케이프하려고 `{{` 사용
```csharp
// 이것도 보간 충돌 발생
string prompt = $@"
예시: {{"tool":"ToolName"}}
";
// 오류: 따옴표 이스케이프가 제대로 안 됨
```

#### 3. 백슬래시로 따옴표 이스케이프 (`\"`)
```csharp
// @"..." 문자열에서는 백슬래시가 특수문자로 취급됨
string prompt = @"예시: {\"tool\":\"ToolName\"}";
// 오류: CS1056 - 예기치 않은 '\' 문자
```

#### 4. `$@""` + `+` 연결로 JSON 삽입 시도
```csharp
string prompt = $@"
설명: " + @"{""tool"":""ToolName""}" + @"
";
// 결과: 문자열이 중간에 끊어지고 파싱 오류 발생
```

---

## ✅ 해결 방법

### 방법 1: 보간 문자열 사용 안 하기 (권장)
JSON을 포함해야 하면 `$@"..."` 대신 `@"..."` 사용:

```csharp
private string BuildSystemPrompt()
{
    var toolDescriptions = string.Join("\n", 
        _tools.Values.Select(t => $"- {t.Name}: {t.Description}"));

    string prompt = @"
너는 웹 페이지에서 작업을 수행하는 에이전트이다.

도구: " + toolDescriptions + @"

예시:
도구 호출: {""tool"":""GetDomSummary"",""args"":""""}
";
    return prompt;
}
```

### 방법 2: `{{` 와 `""` 조합 (JSON이 단순할 때)
보간이 필요하면 중괄호를 이중으로, 따옴표도 이중으로:

```csharp
string prompt = $@"
예시: 
- 도구 호출: {{""tool"":""GetDomSummary"",""args"":""""}}
- 작업 종료: {{""tool"":null,""final"":""완료""}}

변수 삽입: {toolDescriptions}
";
```

**규칙:**
- `$@"..."` 문자열에서 `{` → `{{`로 이중화
- `"` → `""` 로 이중화
- `$` 보간은 정상 작동

### 방법 3: 세 부분으로 나누기 (가장 안전)
```csharp
string prompt = $@"
너는 에이전트이다.

{toolDescriptions}

";  // 보간 끝

// 그 다음 JSON 예시는 일반 문자열로
string examples = @"
도구 호출 예시:
{""tool"":""GetDomSummary"",""args"":""""}
";

string fullPrompt = prompt + examples;
```

---

## 현재 코드의 올바른 형태 (Agent.cs)

```csharp
private string BuildSystemPrompt()
{
    var toolDescriptions = string.Join("\n", 
        _tools.Values.Select(t => $"- {t.Name}: {t.Description}"));

    string prompt = $@"
너는 웹 페이지에서 작업을 수행하는 에이전트이다.
다음과 같은 도구들을 사용할 수 있다:

{toolDescriptions}

====== 사용 예시 ======
사용자: '비밀번호에 4323 입력해줘'

도구 호출 형식: {{""tool"":""InputText"",""args"":""selector:input[name='pw']|4323""}}

====== 중요 규칙 ======
1. 항상 JSON 형식으로만 응답
2. 도구 호출: {{""tool"":""ToolName"",""args"":""arguments""}}
3. 작업 종료: {{""tool"":null,""final"":""요약 문장""}}
";
    return prompt;
}
```

**핵심:**
- `$@"..."` 문자열에서 JSON 예시는 `{{` `}}` `""` 로 이중화
- 변수는 `{변수명}` 으로 정상 보간
- 따옴표는 무조건 `""` 로 이중화

---

## 체크리스트 (다음 번에 체크)

✓ JSON 예시가 포함되는 경우?
  - `$@"..."` 문자열이면 `{{` `""` 이중화 확인
  - 또는 `@"..."` 으로 분리

✓ 특수 문자 포함 문자열?
  - `$@"..."` = 보간 + Verbatim (줄바꿈, 백슬래시 허용)
  - `@"..."` = Verbatim만 (변수 보간 없음)
  - `$"..."` = 보간만

✓ 빌드 오류 "예기치 않은 '\\' 문자" ?
  - `@"..."` 또는 `@$"..."` 문자열에서 `\"` 쓰면 안 됨
  - `""` 로 이중화하거나 보간 문자열 분리

✓ CS1002, CS1010 오류들?
  - 보간 문자열의 중괄호 문제일 가능성 높음
  - `{` → `{{` 로 변경

---

## 참고 자료

C# 문자열 종류:
| 종류 | 문법 | 보간 | 줄바꿈 | 백슬래시 | 따옴표 |
|------|------|------|--------|---------|--------|
| 일반 | `"..."` | O | X | X | `\"` |
| Verbatim | `@"..."` | X | O | 있음 | `""` |
| 보간 | `$"..."` | O | X | X | `\"` |
| 보간+Verbatim | `$@"..."` | O | O | 있음 | `""` |

**따옴표 이스케이프:**
- 보간 문자열 `"..."` `$"..."` : `\"` 사용
- Verbatim `@"..."` `$@"..."` : `""` 사용 (중복)
