using System; // 직렬화 기능 사용
using System.Collections.Generic; // 선택지 목록 사용
using UnityEngine; // 스크립터블 오브젝트 사용

[CreateAssetMenu(
    fileName = "ExplorationEventData",
    menuName = "Project C/Exploration/Event Data")]
public sealed class ExplorationEventData : ScriptableObject // 탐사 이벤트 데이터 정의
{
    [SerializeField] private string eventId = "event_id"; // 이벤트 고유 ID
    [SerializeField] private string displayName = "이벤트"; // 이벤트 표시 이름
    [SerializeField] private string description = "이벤트 설명"; // 이벤트 본문 설명
    [SerializeField] private Sprite illustrationSprite; // 이벤트 패널 일러스트 스프라이트
    [SerializeField] private Sprite worldSprite; // 탐사 맵 이벤트 고정 표시 스프라이트
    [SerializeField] private ExplorationEventCategory category = ExplorationEventCategory.Choice; // 이벤트 분류
    [SerializeField] private List<ExplorationEventChoiceData> choices = new List<ExplorationEventChoiceData>(); // 이벤트 선택지 목록

    public string EventId => eventId; // 이벤트 ID 조회
    public string DisplayName => displayName; // 이벤트 이름 조회
    public string Description => description; // 이벤트 설명 조회
    public Sprite IllustrationSprite => illustrationSprite; // 이벤트 패널 그림 조회
    public Sprite WorldSprite => worldSprite; // 탐사 맵 이벤트 표시 스프라이트 조회
    public ExplorationEventCategory Category => category; // 이벤트 분류 조회
    public IReadOnlyList<ExplorationEventChoiceData> Choices => choices; // 선택지 목록 조회

    public bool IsValidData() // 이벤트 데이터 유효성 확인
    {
        return !string.IsNullOrWhiteSpace(eventId) &&
               !string.IsNullOrWhiteSpace(displayName) &&
               !string.IsNullOrWhiteSpace(description) &&
               choices != null &&
               choices.Count > 0; // 기본 표시와 선택지 존재 여부 확인
    }

    public void Initialize(
        string id,
        string name,
        string text,
        ExplorationEventCategory eventCategory,
        Sprite sprite,
        List<ExplorationEventChoiceData> choiceList) // 런타임 기본 이벤트 데이터 초기화
    {
        eventId = id; // 런타임 이벤트 ID 저장
        displayName = name; // 런타임 이벤트 이름 저장
        description = text; // 런타임 이벤트 설명 저장
        category = eventCategory; // 런타임 이벤트 분류 저장
        illustrationSprite = sprite; // 런타임 이벤트 그림 저장
        choices = choiceList ?? new List<ExplorationEventChoiceData>(); // 런타임 선택지 저장
    }
}

public enum ExplorationEventCategory // 이벤트 분류 열거형
{
    Reward, // 보상 이벤트
    Risk, // 위험 이벤트
    Choice // 선택형 이벤트
}

[Serializable]
public sealed class ExplorationEventChoiceData // 이벤트 선택지 데이터
{
    [SerializeField] private string choiceText = "선택"; // 버튼에 표시할 선택 문구
    [SerializeField] private string resultText = "결과"; // 즉시 적용 결과 문구
    [SerializeField] private bool hasRandomOutcome; // 확률 판정 사용 여부
    [SerializeField] [Range(0, 100)] private int successChancePercent = 100; // 확률 성공치
    [SerializeField] private string successText = "성공"; // 성공 결과 문구
    [SerializeField] private string failureText = "실패"; // 실패 결과 문구
    [SerializeField] private ExplorationEventResourceChange directChange = new ExplorationEventResourceChange(); // 즉시 적용 변화량
    [SerializeField] private ExplorationEventResourceChange successChange = new ExplorationEventResourceChange(); // 성공 변화량
    [SerializeField] private ExplorationEventResourceChange failureChange = new ExplorationEventResourceChange(); // 실패 변화량

    public string ChoiceText => choiceText; // 선택 문구 조회
    public string ResultText => resultText; // 결과 문구 조회
    public bool HasRandomOutcome => hasRandomOutcome; // 확률 사용 여부 조회
    public int SuccessChancePercent => successChancePercent; // 성공 확률 조회
    public string SuccessText => successText; // 성공 문구 조회
    public string FailureText => failureText; // 실패 문구 조회
    public ExplorationEventResourceChange DirectChange => directChange; // 즉시 변화량 조회
    public ExplorationEventResourceChange SuccessChange => successChange; // 성공 변화량 조회
    public ExplorationEventResourceChange FailureChange => failureChange; // 실패 변화량 조회

    public bool IsValidData() // 선택지 데이터 유효성 확인
    {
        return !string.IsNullOrWhiteSpace(choiceText); // 버튼 문구 존재 여부 확인
    }

    public void Initialize(
        string buttonText,
        string directResultText,
        ExplorationEventResourceChange immediateChange) // 즉시 선택지 초기화
    {
        choiceText = buttonText; // 선택 문구 저장
        resultText = directResultText; // 결과 문구 저장
        hasRandomOutcome = false; // 즉시 선택지 사용 설정
        successChancePercent = 100; // 성공 확률 기본값 저장
        successText = string.Empty; // 성공 문구 초기화
        failureText = string.Empty; // 실패 문구 초기화
        directChange = immediateChange ?? new ExplorationEventResourceChange(); // 즉시 변화량 저장
        successChange = new ExplorationEventResourceChange(); // 성공 변화량 초기화
        failureChange = new ExplorationEventResourceChange(); // 실패 변화량 초기화
    }

    public void InitializeRandom(
        string buttonText,
        int successChance,
        string successResultText,
        ExplorationEventResourceChange successResultChange,
        string failureResultText,
        ExplorationEventResourceChange failureResultChange) // 확률 선택지 초기화
    {
        choiceText = buttonText; // 선택 문구 저장
        resultText = string.Empty; // 즉시 결과 문구 초기화
        hasRandomOutcome = true; // 확률 선택지 사용 설정
        successChancePercent = Mathf.Clamp(successChance, 0, 100); // 성공 확률 저장
        successText = successResultText; // 성공 문구 저장
        failureText = failureResultText; // 실패 문구 저장
        directChange = new ExplorationEventResourceChange(); // 즉시 변화량 초기화
        successChange = successResultChange ?? new ExplorationEventResourceChange(); // 성공 변화량 저장
        failureChange = failureResultChange ?? new ExplorationEventResourceChange(); // 실패 변화량 저장
    }
}

[Serializable]
public sealed class ExplorationEventResourceChange // 이벤트 자원 변화량
{
    [SerializeField] private int gold; // 골드 변화량
    [SerializeField] private int screw; // 나사 변화량
    [SerializeField] private int ironPlate; // 철판 변화량
    [SerializeField] private int wire; // 전선 변화량

    public int Gold => gold; // 골드 변화량 조회
    public int Screw => screw; // 나사 변화량 조회
    public int IronPlate => ironPlate; // 철판 변화량 조회
    public int Wire => wire; // 전선 변화량 조회

    public bool IsEmpty() // 변화량 비어 있음 여부 확인
    {
        return gold == 0 &&
               screw == 0 &&
               ironPlate == 0 &&
               wire == 0; // 모든 자원 변화량 0 여부 확인
    }

    public void Initialize(
        int goldAmount,
        int screwAmount,
        int ironPlateAmount,
        int wireAmount) // 변화량 데이터 초기화
    {
        gold = goldAmount; // 골드 변화량 저장
        screw = screwAmount; // 나사 변화량 저장
        ironPlate = ironPlateAmount; // 철판 변화량 저장
        wire = wireAmount; // 전선 변화량 저장
    }

    public string BuildSummary() // 변화량 요약 문구 생성
    {
        List<string> parts = new List<string>(); // 출력 조각 목록 생성

        AppendPart(parts, "골드", gold); // 골드 조각 추가
        AppendPart(parts, "나사", screw); // 나사 조각 추가
        AppendPart(parts, "철판", ironPlate); // 철판 조각 추가
        AppendPart(parts, "전선", wire); // 전선 조각 추가

        return parts.Count > 0
            ? string.Join(", ", parts)
            : "변화 없음"; // 자원 변화 요약 반환
    }

    private static void AppendPart(
        List<string> parts,
        string label,
        int amount) // 변화량 조각 생성
    {
        if (amount == 0)
        {
            return;
        }

        string prefix = amount > 0 ? "+" : string.Empty; // 양수 표기 접두사 계산
        parts.Add($"{label} {prefix}{amount}"); // 요약 조각 추가
    }
}
