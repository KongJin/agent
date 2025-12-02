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
 # C# 문자열 이스케이프 가이드 (간결판)

목표: Agent 코드에 JSON 예시(LLM system/user prompt 포함)를 안전하게 넣는 방법을 정리합니다. 특히 `$@"..."` 같은 복합 문자열에서 생긴 컴파일 오류(CS1002, CS1010, CS1056 등)가 다시 발생하지 않도록 실전 팁과 예제를 제공합니다.

---

## 핵심 원칙 (요약)
- 가능한 한 보간(Interpolation)과 복잡한 JSON 예시를 같은 원시 문자열(verbatim) 안에 섞지 마세요.
- JSON 예시는 따로 분리하거나, verbatim 문자열 내에서 `""`(따옴표 이중화)와 `{{`(중괄호 이중화)를 사용해 안전하게 표기하세요.
- 가장 안전한 방법은: (1) 보간으로 변수만 넣을 부분을 만들고, (2) JSON 예시는 별도의 non-interpolated verbatim 문자열로 둔 뒤 연결(concatenate)합니다.

---

## 권장 패턴 (우선순위)

1) 분리 후 연결 (가장 안전)

```csharp
var toolDescriptions = string.Join("\n", _tools.Values.Select(t => $"- {t.Name}: {t.Description}"));

var header = @"You are a web automation agent.
Use tools to achieve the user's goal.
"; // non-interpolated verbatim

var examples = @"
Examples (JSON only):
{""tool"": ""GetDomSummary"", ""args"": """" }
{""tool"": ""InputText"", ""args"": ""selector:input[name='q']|text|enter=true"" }
"; // non-interpolated verbatim

var prompt = header + "\n" + toolDescriptions + "\n" + examples;
```

장점: 매우 직관적이고 안전. 복잡한 따옴표/중괄호 충돌이 거의 없음.

2) verbatim 보간 사용 시: JSON 블록은 이중화 규칙 적용

```csharp
var prompt = $@"Header text
{toolDescriptions}

예시: {{""tool"":""InputText"",""args"":""selector:input[name='pw']|4323""}}
";
```

규칙: `$@"..."` 안에서
- `{` → `{{`
- `}` → `}}`
- `"` → `""`

3) 절대 쓰지 말아야 할 패턴
- `$@"...{"tool":"x"}..."` 같이 중괄호/따옴표 혼용.
- `@"...\"..."` (verbatim에서 백슬래시로 따옴표 이스케이프) — verbatim은 `""`를 사용해야 합니다.

---

## 예제: Agent에서 안전하게 prompt 만들기

안전한 구현(권장):

```csharp
private string BuildSystemPrompt()
{
  var toolDescriptions = string.Join("\n", _tools.Values.Select(t => $"- {t.Name}: {t.Description}"));

  var header = @"You are a web automation agent that controls browser tools to accomplish user goals.
Available tools:";

  var examples = @"
Examples (JSON only):
{""tool"":""GetDomSummary"",""args"":""""}
{""tool"":""ClickElement"",""args"":""selector:#loginBtn""}
{""tool"":null,""final"":""Done""}
";

  return header + "\n" + toolDescriptions + "\n" + examples;
}
```

부분 보간(주의해서 사용):

```csharp
// toolDescriptions는 변수로 보간, JSON 예시는 이중화
var prompt = $@"Header info
{toolDescriptions}

예시: {{""tool"":""InputText"",""args"":""selector:input[name='q']|text""}}
";
```

---

## 도구별 JSON 예시 (권장 표기)
- InputText: `{ ""tool"": ""InputText"", ""args"": ""selector:input[name='q']|검색어|enter=true"" }`
- ClickElement: `{ ""tool"": ""ClickElement"", ""args"": ""selector:#loginBtn"" }`
- CloseTab: `{ ""tool"": ""CloseTab"", ""args"": """" }`
- Navigate: `{ ""tool"": ""Navigate"", ""args"": ""back"" }`
- MoveMouse: `{ ""tool"": ""MoveMouse"", ""args"": ""x:100|y:200"" }`

위 예시들은 모두 `@"..."` (verbatim) 블록 안에 넣을 때 `""` 형태로 표기하세요.

---

## 빠른 체크리스트 (빌드 전에)
- [ ] `$@"..."` 안에 JSON이 있다면 `""`/`{{` 이중화 확인
- [ ] 복잡하면 JSON 예시를 분리해서 `+`로 이어붙이기
- [ ] `@"..."` 안에서 `\"` 사용하지 않기 (대신 `""`)
- [ ] 빌드 실패 시 컴파일 에러 라인(예: CS1002) 확인 → 보간 중괄호 문제 확률 높음

---

필요하면 제가 `Agent.BuildSystemPrompt`와 `Agent.BuildUserPrompt`를 위 규칙에 맞게 안전하게 재작성해 드리겠습니다.
