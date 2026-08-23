using System.Text; // 계산식 문자열 구성 기능 사용
using UnityEngine; // 개발용 IMGUI 기능 사용
using UnityEngine.InputSystem; // New Input System 키보드 입력 사용
using UnityEngine.SceneManagement; // 현재 Scene 확인 기능 사용

public sealed class BattleDamageDebugView : MonoBehaviour // 카드 피해 계산 디버그 화면
{
    private const string BattleSceneName = "40_Battle"; // 전투 Scene 이름

    private static BattleDamageDebugView instance; // 디버그 화면 인스턴스

    private readonly StringBuilder calculationText =
        new StringBuilder(); // 계산식 문자열 저장소

    private bool debugEnabled = true; // 디버그 화면 사용 여부
    private Vector2 scrollPosition; // 계산식 스크롤 위치
    private GUIStyle headerLabelStyle; // 제목 표시 스타일
    private GUIStyle bodyLabelStyle; // 본문 표시 스타일
    private GUIStyle hintLabelStyle; // 안내 표시 스타일
    private CardInstance currentCard; // 현재 계산 카드
    private int expectedTargetCount; // 현재 카드 대상 수
    private int recordedTargetCount; // 기록 완료 대상 수

    public static BattleDamageDebugView EnsureInstance() // 디버그 화면 존재 보장
    {
        if (instance != null)
        {
            return instance;
        }

        instance =
            FindFirstObjectByType<BattleDamageDebugView>(); // 기존 화면 탐색

        if (instance != null)
        {
            return instance;
        }

        GameObject debugObject =
            new GameObject(nameof(BattleDamageDebugView)); // 디버그 화면 오브젝트 생성

        instance =
            debugObject.AddComponent<BattleDamageDebugView>(); // 디버그 컴포넌트 추가

        DontDestroyOnLoad(debugObject); // Scene 이동 중 토글 상태 유지

        return instance;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeRuntime() // 디버그 화면 자동 준비
    {
        EnsureInstance(); // 디버그 화면 생성
    }

    private void Awake() // 디버그 화면 초기화
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject); // 중복 디버그 화면 제거
            return;
        }

        instance = this; // 현재 디버그 화면 등록
        DontDestroyOnLoad(gameObject); // Scene 이동 유지
    }

    private void Update() // 디버그 단축키 입력 처리
    {
        if (SceneManager.GetActiveScene().name != BattleSceneName)
        {
            return;
        }

        Keyboard keyboard =
            Keyboard.current; // 현재 키보드 조회

        if (keyboard == null ||
            !keyboard.f6Key.wasPressedThisFrame)
        {
            return;
        }

        debugEnabled = !debugEnabled; // F6로 디버그 표시 토글

        Debug.Log(
            debugEnabled
                ? "[Battle][Day46] F6 피해 계산 DEBUG UI ON"
                : "[Battle][Day46] F6 피해 계산 DEBUG UI OFF"); // 토글 상태 로그
    }

    public void BeginCard(
        CardInstance card,
        int targetCount) // 카드 피해 계산 표시 시작
    {
        currentCard = card; // 현재 카드 저장
        expectedTargetCount = Mathf.Max(0, targetCount); // 대상 수 저장
        recordedTargetCount = 0; // 기록 대상 수 초기화
        calculationText.Clear(); // 이전 계산식 제거
        scrollPosition = Vector2.zero; // 스크롤 초기화

        if (card == null)
        {
            return;
        }

        calculationText.AppendLine("[46일차 피해 계산 DEBUG]"); // 제목 기록
        calculationText.AppendLine($"카드 : {card.DisplayName}"); // 카드 이름 기록
        calculationText.AppendLine($"태그 : {GetTagLabel(card.SourceData.CardTags)}"); // 카드 태그 기록
        calculationText.AppendLine($"속성 : {GetCardTypeLabel(card.CardType)}"); // 카드 속성 기록
        calculationText.AppendLine($"피해 유형 : {GetDamageTypeLabel(card.DamageType)}"); // 피해 유형 기록
        calculationText.AppendLine($"대상 : {expectedTargetCount}명"); // 대상 수 기록
        calculationText.AppendLine(); // 구역 여백 기록

    }

    public void RecordCardDamage(
        CardInstance card,
        BattleUnitRuntime target,
        BattleDamageResult result) // 대상별 카드 피해 계산식 기록
    {
        if (card == null || target == null)
        {
            return;
        }

        if (currentCard != card)
        {
            BeginCard(card, 1); // 비정상 순서에서도 계산식 헤더 복원
        }

        recordedTargetCount += 1; // 기록 대상 수 증가

        int cardBaseDamage =
            card.EffectValue; // 카드 원본 효과값 조회

        int attackPowerBonus =
            card.OwnerUnit == null
                ? 0
                : card.OwnerUnit.AttackPowerBonus; // 카드 소유자 공격 보너스 조회

        int beforeMentalDamage =
            Mathf.Max(
                0,
                cardBaseDamage + attackPowerBonus); // 정신 보정 전 공격값 계산

        int predictedAppliedDamage =
            Mathf.Min(
                target.CurrentHealth,
                result.FinalDamage); // 현재 체력 기준 실제 적용 피해 계산

        string defenseName =
            result.DamageType == BattleDamageType.Physical
                ? "물리 방어"
                : result.DamageType == BattleDamageType.Magical
                    ? "마법 저항"
                    : "방어"; // 적용 방어 이름 계산

        calculationText.AppendLine(
            $"[{recordedTargetCount}/{Mathf.Max(1, expectedTargetCount)}] {target.DisplayName}"); // 대상 제목 기록

        calculationText.AppendLine(
            $"카드값 {cardBaseDamage} + 공격 보너스 {attackPowerBonus} = {beforeMentalDamage}"); // 기본 공격 계산 기록

        calculationText.AppendLine(
            $"정신 상태 보정 : {beforeMentalDamage} → {result.RawDamage}"); // 정신 상태 보정 기록

        calculationText.AppendLine(
            $"{defenseName} {result.DefenseValue}"); // 방어값 기록

        calculationText.AppendLine(
            $"{result.RawDamage} - {result.DefenseValue} = {result.DefenseAdjustedDamage}"); // 방어 계산식 기록

        calculationText.AppendLine(
            result.IsWeakness
                ? $"약점 {GetCardTypeLabel(card.CardType)} : YES ×{result.WeaknessMultiplier:0.00}"
                : $"약점 {GetCardTypeLabel(card.CardType)} : NO ×1.00"); // 약점 판정 기록

        calculationText.AppendLine(
            $"최종 Round({result.DefenseAdjustedDamage} × {result.WeaknessMultiplier:0.00}) = {result.FinalDamage}"); // 최종 계산식 기록

        calculationText.AppendLine(
            $"적용 피해 min(현재 HP {target.CurrentHealth}, {result.FinalDamage}) = {predictedAppliedDamage}"); // 실제 적용 피해 기록

        calculationText.AppendLine(); // 대상 사이 여백 기록

    }

    private void OnGUI() // 왼쪽 아래 피해 계산 창 출력
    {
        if (!debugEnabled ||
            SceneManager.GetActiveScene().name != BattleSceneName ||
            calculationText.Length == 0)
        {
            return;
        }

        EnsureGuiStyles(); // 큰 글자 표시용 스타일 준비

        float width =
            Mathf.Min(560f, Screen.width - 40f); // 왼쪽 아래 창 너비 계산

        float height =
            Mathf.Min(300f, Screen.height - 40f); // 왼쪽 아래 창 높이 계산

        Rect panelRect =
            new Rect(
                18f,
                Screen.height - height - 18f,
                width,
                height); // 왼쪽 아래 창 위치 계산

        GUILayout.BeginArea(
            panelRect,
            GUI.skin.window); // 계산 디버그 창 시작

        GUILayout.Label(
            "F6 : 피해 계산 DEBUG UI 켜기 / 끄기",
            headerLabelStyle); // 단축키 안내 출력

        GUILayout.Space(6f); // 안내와 본문 사이 여백

        GUILayout.Label(
            "카드 사용 시 계산 과정을 표시합니다.",
            hintLabelStyle); // 보조 안내 출력

        GUILayout.Space(8f); // 안내와 계산식 사이 여백

        scrollPosition =
            GUILayout.BeginScrollView(
                scrollPosition); // 광역 카드 계산식 스크롤 시작

        GUILayout.Label(
            calculationText.ToString(),
            bodyLabelStyle); // 계산식 출력

        GUILayout.EndScrollView(); // 계산식 스크롤 종료
        GUILayout.EndArea(); // 계산 디버그 창 종료
    }

    private void EnsureGuiStyles() // 디버그 창 큰 글자 스타일 준비
    {
        if (headerLabelStyle != null &&
            bodyLabelStyle != null &&
            hintLabelStyle != null)
        {
            return;
        }

        headerLabelStyle =
            new GUIStyle(GUI.skin.label); // 제목 스타일 생성

        headerLabelStyle.fontSize = 20; // 제목 글자 크기 확대
        headerLabelStyle.fontStyle = FontStyle.Bold; // 제목 굵게 표시
        headerLabelStyle.wordWrap = true; // 제목 줄바꿈 허용

        hintLabelStyle =
            new GUIStyle(GUI.skin.label); // 안내 스타일 생성

        hintLabelStyle.fontSize = 16; // 안내 글자 크기 확대
        hintLabelStyle.wordWrap = true; // 안내 줄바꿈 허용

        bodyLabelStyle =
            new GUIStyle(GUI.skin.label); // 본문 스타일 생성

        bodyLabelStyle.fontSize = 18; // 본문 글자 크기 확대
        bodyLabelStyle.wordWrap = true; // 본문 줄바꿈 허용
        bodyLabelStyle.richText = false; // 일반 텍스트 출력 유지
    }

    private static string GetTagLabel(
        CardTag tags) // 카드 태그 표시 문구 생성
    {
        if (tags == CardTag.None)
        {
            return "None";
        }

        StringBuilder label =
            new StringBuilder(); // 태그 문구 생성기

        AppendTagLabel(
            label,
            tags,
            CardTag.Attack,
            "Attack"); // 공격 태그 추가

        AppendTagLabel(
            label,
            tags,
            CardTag.Magic,
            "Magic"); // 마법 태그 추가

        AppendTagLabel(
            label,
            tags,
            CardTag.Skill,
            "Skill"); // 스킬 태그 추가

        return label.ToString(); // 완성 태그 문구 반환
    }

    private static void AppendTagLabel(
        StringBuilder label,
        CardTag tags,
        CardTag targetTag,
        string targetLabel) // 태그 문구 한 항목 추가
    {
        if ((tags & targetTag) == 0)
        {
            return;
        }

        if (label.Length > 0)
        {
            label.Append(" | "); // 태그 구분자 추가
        }

        label.Append(targetLabel); // 태그 이름 추가
    }

    private static string GetCardTypeLabel(
        CardType cardType) // 카드 속성 한글 이름 조회
    {
        switch (cardType)
        {
            case CardType.Sword:
                return "검";

            case CardType.Wand:
                return "지팡이";

            case CardType.Cup:
                return "성배";

            case CardType.Pentacle:
                return "팬타클";

            case CardType.Shield:
                return "방패";

            default:
                return cardType.ToString();
        }
    }

    private static string GetDamageTypeLabel(
        BattleDamageType damageType) // 피해 유형 한글 이름 조회
    {
        switch (damageType)
        {
            case BattleDamageType.Physical:
                return "Physical / 물리";

            case BattleDamageType.Magical:
                return "Magical / 마법";

            default:
                return "None / 일반";
        }
    }

    private void OnDestroy() // 디버그 화면 제거 처리
    {
        if (instance == this)
        {
            instance = null; // 정적 인스턴스 제거
        }
    }
}
