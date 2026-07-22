using TMPro; // TextMesh Pro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

public sealed class BattleUnitHealthUI : MonoBehaviour // 전투 유닛 체력 UI 관리자
{
    [Header("전투 유닛")] // 전투 유닛 구분
    [SerializeField] private BattleUnit battleUnit; // 표시 대상 전투 유닛

    [Header("체력 UI")] // 체력 UI 구분
    [SerializeField] private TMP_Text unitNameText; // 유닛 이름 문구
    [SerializeField] private Slider healthSlider; // 체력 슬라이더
    [SerializeField] private TMP_Text healthValueText; // 체력 수치 문구

    private void OnEnable() // 체력 이벤트 연결
    {
        if (battleUnit != null) // 전투 유닛 연결 확인
        {
            battleUnit.HealthChanged += RefreshDisplay; // 체력 변경 이벤트 등록
        }
    }

    private void Start() // 체력 UI 초기화
    {
        if (battleUnit == null) // 전투 유닛 연결 확인
        {
            Debug.LogError("BattleUnit이 연결되지 않았습니다.", this); // 유닛 누락 오류
            return; // UI 초기화 중단
        }

        if (unitNameText != null) // 이름 문구 연결 확인
        {
            unitNameText.text = battleUnit.UnitName; // 유닛 이름 표시
        }

        RefreshDisplay(battleUnit.CurrentHealth, battleUnit.MaxHealth); // 초기 체력 표시
    }

    private void OnDisable() // 체력 이벤트 해제
    {
        if (battleUnit != null) // 전투 유닛 연결 확인
        {
            battleUnit.HealthChanged -= RefreshDisplay; // 체력 변경 이벤트 해제
        }
    }

    private void RefreshDisplay(int currentHealth, int maximumHealth) // 체력 UI 갱신
    {
        if (healthSlider != null) // 체력 슬라이더 연결 확인
        {
            healthSlider.minValue = 0f; // 체력 슬라이더 최솟값 설정
            healthSlider.maxValue = maximumHealth; // 체력 슬라이더 최댓값 설정
            healthSlider.value = currentHealth; // 현재 체력 슬라이더 반영
        }

        if (healthValueText != null) // 체력 문구 연결 확인
        {
            healthValueText.text = $"{currentHealth} / {maximumHealth}"; // 현재 체력과 최대 체력 표시
        }
    }
}