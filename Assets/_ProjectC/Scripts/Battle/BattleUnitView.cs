using System; // 기본 이벤트 기능 사용
using TMPro; // 텍스트 메시 기능 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.EventSystems; // 유니티 포인터 이벤트 사용
using UnityEngine.UI; // 유니티 UI 기능 사용
public sealed class BattleUnitView : MonoBehaviour, IPointerClickHandler // 전투 유닛 화면 표시
{ // 클래스 시작
    [Header("기본 표시")] // 기본 표시 구역
    [SerializeField] private Image portraitImage; // 초상화 이미지
    [SerializeField] private TMP_Text nameText; // 유닛 이름 텍스트
    [SerializeField] private Image teamFrameImage; // 진영 테두리 이미지
    [Header("체력 표시")] // 체력 표시 구역
    [SerializeField] private Slider healthSlider; // 체력 게이지
    [SerializeField] private TMP_Text healthText; // 체력 숫자 텍스트
    [SerializeField] private GameObject deadMarker; // 사망 표시 오브젝트
    [Header("진영 색상")] // 진영 색상 구역
    [SerializeField] private Color allyColor = new Color(0.2f, 0.55f, 1f, 1f); // 아군 표시 색상
    [SerializeField] private Color enemyColor = new Color(1f, 0.25f, 0.25f, 1f); // 적 표시 색상
    private GameObject enemyIntentRoot; // 적 행동 예고 오브젝트
    private TMP_Text enemyIntentText; // 적 행동 예고 텍스트
    private BattleCombatFeedbackView combatFeedbackView; // 전투 결과 피드백 화면
    public BattleUnitRuntime RuntimeUnit { get; private set; } // 연결된 런타임 유닛
    public event Action<BattleUnitRuntime> Clicked; // 유닛 클릭 이벤트
    public void Bind(BattleUnitRuntime runtimeUnit) // 런타임 유닛 연결
    { // 연결 시작
        if (runtimeUnit == null) // 런타임 유닛 누락 확인
        { // 누락 처리 시작
            Debug.LogError("[BattleUnitView] 연결할 런타임 유닛이 없습니다.", this); // 누락 오류 출력
            return; // 연결 중단
        } // 누락 처리 종료
        Unbind(); // 기존 연결 해제
        RuntimeUnit = runtimeUnit; // 새 런타임 유닛 저장
        RuntimeUnit.HealthChanged += HandleHealthChanged; // 체력 변경 이벤트 등록
        RuntimeUnit.DamageTaken += HandleDamageTaken; // 피해 적용 이벤트 등록
        RuntimeUnit.HealthRestored += HandleHealthRestored; // 회복 적용 이벤트 등록
        RuntimeUnit.Died += HandleDied; // 사망 이벤트 등록
        EnsureCombatFeedbackView(); // 전투 결과 피드백 준비
        ApplyStaticData(); // 고정 표시 정보 적용
        RefreshHealth(); // 체력 표시 갱신
        SetEnemyIntent(null); // 행동 예고 초기화
    } // 연결 종료
    public void Unbind() // 런타임 유닛 연결 해제
    { // 연결 해제 시작
        if (RuntimeUnit == null) // 기존 연결 여부 확인
        { // 미연결 처리 시작
            return; // 연결 해제 중단
        } // 미연결 처리 종료
        RuntimeUnit.HealthChanged -= HandleHealthChanged; // 체력 변경 이벤트 해제
        RuntimeUnit.DamageTaken -= HandleDamageTaken; // 피해 적용 이벤트 해제
        RuntimeUnit.HealthRestored -= HandleHealthRestored; // 회복 적용 이벤트 해제
        RuntimeUnit.Died -= HandleDied; // 사망 이벤트 해제
        RuntimeUnit = null; // 런타임 참조 제거
        if (enemyIntentRoot != null) // 행동 예고 오브젝트 확인
        { // 행동 예고 숨김 시작
            enemyIntentRoot.SetActive(false); // 행동 예고 숨김
        } // 행동 예고 숨김 종료
    } // 연결 해제 종료
    public void SetEnemyIntent(BattleEnemyAction action) // 적 행동 예고 표시
    { // 행동 예고 표시 시작
        if (action == null || RuntimeUnit == null || RuntimeUnit.Team != BattleTeam.Enemy || action.Actor != RuntimeUnit) // 표시 행동 유효성 확인
        { // 행동 예고 숨김 시작
            if (enemyIntentRoot != null) // 행동 예고 오브젝트 확인
            { // 기존 예고 처리 시작
                enemyIntentRoot.SetActive(false); // 행동 예고 숨김
            } // 기존 예고 처리 종료
            return; // 행동 예고 표시 중단
        } // 행동 예고 숨김 종료
        EnsureEnemyIntentView(); // 행동 예고 화면 준비
        string damageLabel = action.DamageType == BattleDamageType.Magical ? "마법" : action.DamageType == BattleDamageType.Physical ? "물리" : "일반"; // 피해 유형 이름 계산
        BattleDamageResult previewResult = action.PreviewDamage(); // 대상 방어력 포함 예상 피해 계산
        enemyIntentText.text = $"예고: {damageLabel} {action.Amount} → {previewResult.AppliedDamage}\n→ {action.Target.DisplayName}"; // 행동 예고 내용 적용
        enemyIntentRoot.SetActive(true); // 행동 예고 표시
        enemyIntentRoot.transform.SetAsLastSibling(); // 행동 예고 최상단 배치
    } // 행동 예고 표시 종료
    public void SetTargetable(bool targetable) // 대상 선택 가능 표시
    { // 대상 표시 시작
        if (teamFrameImage == null || RuntimeUnit == null) // 화면 연결 상태 확인
        { // 연결 없음 처리 시작
            return; // 대상 표시 중단
        } // 연결 없음 처리 종료
        Color teamColor = RuntimeUnit.Team == BattleTeam.Ally ? allyColor : enemyColor; // 기본 진영 색상 계산
        teamFrameImage.color = targetable ? Color.yellow : teamColor; // 대상 가능 색상 적용
    } // 대상 표시 종료
    public void OnPointerClick(PointerEventData eventData) // 유닛 포인터 클릭 처리
    { // 포인터 클릭 처리 시작
        if (RuntimeUnit == null || RuntimeUnit.IsDead) // 유닛 연결과 생존 상태 확인
        { // 클릭 불가 처리 시작
            return; // 포인터 클릭 처리 중단
        } // 클릭 불가 처리 종료
        Clicked?.Invoke(RuntimeUnit); // 유닛 클릭 이벤트 알림
    } // 포인터 클릭 처리 종료
    private void ApplyStaticData() // 고정 표시 정보 적용
    { // 고정 정보 적용 시작
        if (nameText != null) // 이름 텍스트 존재 확인
        { // 이름 적용 시작
            nameText.text = RuntimeUnit.DisplayName; // 유닛 이름 표시
        } // 이름 적용 종료
        if (portraitImage != null) // 초상화 이미지 존재 확인
        { // 초상화 적용 시작
            portraitImage.sprite = RuntimeUnit.Portrait; // 초상화 스프라이트 적용
            portraitImage.enabled = RuntimeUnit.Portrait != null; // 초상화 표시 여부 적용
        } // 초상화 적용 종료
        if (teamFrameImage != null) // 진영 테두리 존재 확인
        { // 진영 색상 적용 시작
            teamFrameImage.color = RuntimeUnit.Team == BattleTeam.Ally ? allyColor : enemyColor; // 진영별 색상 적용
        } // 진영 색상 적용 종료
    } // 고정 정보 적용 종료
    private void RefreshHealth() // 체력 표시 갱신
    { // 체력 갱신 시작
        if (RuntimeUnit == null) // 런타임 유닛 존재 확인
        { // 미연결 처리 시작
            return; // 체력 갱신 중단
        } // 미연결 처리 종료
        if (healthSlider != null) // 체력 게이지 존재 확인
        { // 게이지 갱신 시작
            healthSlider.minValue = 0f; // 게이지 최소값 적용
            healthSlider.maxValue = RuntimeUnit.MaxHealth; // 게이지 최대값 적용
            healthSlider.value = RuntimeUnit.CurrentHealth; // 게이지 현재값 적용
        } // 게이지 갱신 종료
        if (healthText != null) // 체력 텍스트 존재 확인
        { // 체력 숫자 갱신 시작
            healthText.text = $"{RuntimeUnit.CurrentHealth} / {RuntimeUnit.MaxHealth}"; // 현재 체력 표시
        } // 체력 숫자 갱신 종료
        if (deadMarker != null) // 사망 표시 존재 확인
        { // 사망 표시 갱신 시작
            deadMarker.SetActive(RuntimeUnit.IsDead); // 사망 여부 적용
        } // 사망 표시 갱신 종료
    } // 체력 갱신 종료
    private void HandleHealthChanged(BattleUnitRuntime runtimeUnit) // 체력 변경 이벤트 처리
    { // 체력 이벤트 처리 시작
        if (runtimeUnit != RuntimeUnit) // 다른 유닛 이벤트 확인
        { // 다른 유닛 처리 시작
            return; // 체력 이벤트 무시
        } // 다른 유닛 처리 종료
        RefreshHealth(); // 체력 표시 갱신
    } // 체력 이벤트 처리 종료
    private void HandleDamageTaken(BattleUnitRuntime runtimeUnit, BattleDamageResult damageResult) // 피해 적용 이벤트 처리
    { // 피해 이벤트 처리 시작
        if (runtimeUnit != RuntimeUnit || combatFeedbackView == null) // 연결 유닛과 피드백 화면 확인
        { // 다른 유닛 처리 시작
            return; // 피해 이벤트 무시
        } // 다른 유닛 처리 종료
        combatFeedbackView.ShowDamage(damageResult); // 피해 숫자와 강조 표시
    } // 피해 이벤트 처리 종료
    private void HandleHealthRestored(BattleUnitRuntime runtimeUnit, int appliedHealing) // 회복 적용 이벤트 처리
    { // 회복 이벤트 처리 시작
        if (runtimeUnit != RuntimeUnit || combatFeedbackView == null) // 연결 유닛과 피드백 화면 확인
        { // 다른 유닛 처리 시작
            return; // 회복 이벤트 무시
        } // 다른 유닛 처리 종료
        combatFeedbackView.ShowHealing(appliedHealing); // 회복 숫자와 강조 표시
    } // 회복 이벤트 처리 종료
    private void HandleDied(BattleUnitRuntime runtimeUnit) // 사망 이벤트 처리
    { // 사망 이벤트 처리 시작
        if (runtimeUnit != RuntimeUnit) // 다른 유닛 이벤트 확인
        { // 다른 유닛 처리 시작
            return; // 사망 이벤트 무시
        } // 다른 유닛 처리 종료
        RefreshHealth(); // 사망 표시 갱신
        if (enemyIntentRoot != null) // 행동 예고 오브젝트 확인
        { // 행동 예고 숨김 시작
            enemyIntentRoot.SetActive(false); // 사망 유닛 예고 숨김
        } // 행동 예고 숨김 종료
    } // 사망 이벤트 처리 종료
    private void EnsureEnemyIntentView() // 적 행동 예고 화면 준비
    { // 예고 화면 준비 시작
        if (enemyIntentRoot != null) // 기존 예고 화면 확인
        { // 기존 화면 처리 시작
            return; // 중복 생성 중단
        } // 기존 화면 처리 종료
        enemyIntentRoot = new GameObject("EnemyIntent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 행동 예고 배경 생성
        enemyIntentRoot.transform.SetParent(transform, false); // 유닛 화면 자식 배치
        RectTransform intentRect = enemyIntentRoot.GetComponent<RectTransform>(); // 행동 예고 사각형 조회
        intentRect.anchorMin = new Vector2(0.5f, 1f); // 상단 중앙 최소 앵커
        intentRect.anchorMax = new Vector2(0.5f, 1f); // 상단 중앙 최대 앵커
        intentRect.pivot = new Vector2(0.5f, 1f); // 상단 중앙 기준점
        intentRect.anchoredPosition = new Vector2(0f, -6f); // 유닛 내부 상단 위치
        intentRect.sizeDelta = new Vector2(165f, 40f); // 행동 예고 크기
        Image intentBackground = enemyIntentRoot.GetComponent<Image>(); // 행동 예고 배경 조회
        intentBackground.color = new Color(0.18f, 0.06f, 0.06f, 0.94f); // 행동 예고 배경색 적용
        intentBackground.raycastTarget = false; // 행동 예고 클릭 차단 해제
        GameObject textObject = new GameObject("IntentText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 행동 예고 글자 생성
        textObject.transform.SetParent(enemyIntentRoot.transform, false); // 글자 배경 자식 배치
        RectTransform textRect = textObject.GetComponent<RectTransform>(); // 글자 사각형 조회
        textRect.anchorMin = Vector2.zero; // 글자 최소 앵커 적용
        textRect.anchorMax = Vector2.one; // 글자 최대 앵커 적용
        textRect.offsetMin = new Vector2(5f, 3f); // 글자 왼쪽 아래 여백
        textRect.offsetMax = new Vector2(-5f, -3f); // 글자 오른쪽 위 여백
        enemyIntentText = textObject.GetComponent<TextMeshProUGUI>(); // 행동 예고 텍스트 조회
        enemyIntentText.font = ProjectCFontProvider.KoreanFontAsset; // 한글 지원 글꼴 적용
        enemyIntentText.fontSize = 13f; // 행동 예고 글자 크기
        enemyIntentText.color = new Color(1f, 0.78f, 0.42f, 1f); // 행동 예고 글자색 적용
        enemyIntentText.alignment = TextAlignmentOptions.Center; // 행동 예고 가운데 정렬
        enemyIntentText.textWrappingMode = TextWrappingModes.NoWrap; // 행동 예고 자동 줄바꿈 해제
        enemyIntentText.raycastTarget = false; // 행동 예고 글자 클릭 차단 해제
        enemyIntentRoot.SetActive(false); // 행동 예고 기본 숨김
    } // 예고 화면 준비 종료
    private void EnsureCombatFeedbackView() // 전투 결과 피드백 준비
    { // 피드백 준비 시작
        if (combatFeedbackView != null) // 기존 피드백 화면 확인
        { // 기존 피드백 처리 시작
            return; // 중복 준비 중단
        } // 기존 피드백 처리 종료
        combatFeedbackView = GetComponent<BattleCombatFeedbackView>(); // 기존 피드백 컴포넌트 조회
        if (combatFeedbackView == null) // 피드백 컴포넌트 누락 확인
        { // 피드백 컴포넌트 생성 시작
            combatFeedbackView = gameObject.AddComponent<BattleCombatFeedbackView>(); // 런타임 피드백 컴포넌트 추가
        } // 피드백 컴포넌트 생성 종료
    } // 피드백 준비 종료
    private void OnDestroy() // 오브젝트 제거 처리
    { // 제거 처리 시작
        Unbind(); // 이벤트 연결 해제
    } // 제거 처리 종료
} // 클래스 종료
