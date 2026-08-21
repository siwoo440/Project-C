using TMPro; // 텍스트 메시 기능 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.UI; // 유니티 UI 기능 사용
public sealed class BattleUnitView : MonoBehaviour // 전투 유닛 화면 표시
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
    public BattleUnitRuntime RuntimeUnit { get; private set; } // 연결된 런타임 유닛
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
        RuntimeUnit.Died += HandleDied; // 사망 이벤트 등록
        ApplyStaticData(); // 고정 표시 정보 적용
        RefreshHealth(); // 체력 표시 갱신
    } // 연결 종료
    public void Unbind() // 런타임 유닛 연결 해제
    { // 연결 해제 시작
        if (RuntimeUnit == null) // 기존 연결 여부 확인
        { // 미연결 처리 시작
            return; // 연결 해제 중단
        } // 미연결 처리 종료
        RuntimeUnit.HealthChanged -= HandleHealthChanged; // 체력 변경 이벤트 해제
        RuntimeUnit.Died -= HandleDied; // 사망 이벤트 해제
        RuntimeUnit = null; // 런타임 참조 제거
    } // 연결 해제 종료
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
    private void HandleDied(BattleUnitRuntime runtimeUnit) // 사망 이벤트 처리
    { // 사망 이벤트 처리 시작
        if (runtimeUnit != RuntimeUnit) // 다른 유닛 이벤트 확인
        { // 다른 유닛 처리 시작
            return; // 사망 이벤트 무시
        } // 다른 유닛 처리 종료
        RefreshHealth(); // 사망 표시 갱신
    } // 사망 이벤트 처리 종료
    private void OnDestroy() // 오브젝트 제거 처리
    { // 제거 처리 시작
        Unbind(); // 이벤트 연결 해제
    } // 제거 처리 종료
} // 클래스 종료
