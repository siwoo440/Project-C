using UnityEngine; // 시간과 디버그 기능 사용

public sealed class ExplorationHazardRuntime : MonoBehaviour // 탐사 퇴색 노출과 환경 피해 처리
{
    private const float MaximumExposure = 100f; // 환경 피해 발동 최대 노출도

    private ExplorationMapRuntime mapRuntime; // 현재 탐사 맵 런타임
    private ExplorationSessionManager sessionManager; // 탐사 세션 관리자
    private ExplorationHazardRoomState currentHazard; // 현재 플레이어 방 위험 상태
    private Vector2Int currentRoomCoordinate; // 현재 플레이어 방 좌표
    private float exposure; // 현재 퇴색 노출도
    private float lastPulseTime = -100f; // 마지막 환경 피해 시각
    private int lastHealthDamage; // 마지막 환경 체력 피해
    private int lastMentalDamage; // 마지막 환경 정신력 피해

    public bool IsInHazard =>
        currentHazard != null &&
        currentHazard.HazardType == ExplorationHazardType.Fade; // 현재 퇴색 지역 여부

    public int CurrentHazardLevel =>
        IsInHazard
            ? currentHazard.Level
            : 0; // 현재 위험도 조회

    public float Exposure =>
        exposure; // 현재 노출도 조회

    public float ExposureNormalized =>
        Mathf.Clamp01(
            exposure /
            MaximumExposure); // 0~1 노출도 조회

    public Vector2Int CurrentRoomCoordinate =>
        currentRoomCoordinate; // 현재 방 좌표 조회

    public int LastHealthDamage =>
        lastHealthDamage; // 마지막 체력 피해 조회

    public int LastMentalDamage =>
        lastMentalDamage; // 마지막 정신력 피해 조회

    public float LastPulseTime =>
        lastPulseTime; // 마지막 피해 시각 조회

    public void Configure(
        ExplorationMapRuntime targetMapRuntime,
        ExplorationSessionManager targetSessionManager) // 위험 런타임 연결
    {
        mapRuntime =
            targetMapRuntime; // 탐사 맵 런타임 저장

        sessionManager =
            targetSessionManager; // 탐사 세션 관리자 저장
    }

    public void ResetForCurrentFloor() // 층 변경 시 현재 방 판정 초기화
    {
        currentHazard = null; // 현재 방 위험 상태 초기화
        currentRoomCoordinate = Vector2Int.zero; // 현재 방 좌표 초기화
    }

    private void Update() // 플레이어 퇴색 지역 체류와 노출도 처리
    {
        if (mapRuntime == null ||
            sessionManager == null ||
            sessionManager.IsExplorationCompleted ||
            ExplorationPlayerController.InputBlocked)
        {
            return; // 탐사 완료 또는 이벤트 UI 중 환경 노출 일시 정지
        }

        if (!mapRuntime.TryGetPlayerRoomCoordinate(
                out Vector2Int roomCoordinate))
        {
            currentHazard = null; // 통로에서는 퇴색 방 판정 해제
            return;
        }

        currentRoomCoordinate =
            roomCoordinate; // 현재 플레이어 방 좌표 갱신

        if (!mapRuntime.TryGetHazardAt(
                roomCoordinate,
                out ExplorationHazardRoomState hazardState) ||
            hazardState == null ||
            hazardState.HazardType != ExplorationHazardType.Fade)
        {
            currentHazard = null; // 안전 방에서는 노출 증가 중지
            return;
        }

        currentHazard =
            hazardState; // 현재 퇴색 위험 상태 저장

        exposure +=
            GetExposurePerSecond(
                hazardState.Level) *
            Time.deltaTime; // 위험도별 퇴색 노출 누적

        while (exposure >=
               MaximumExposure)
        {
            exposure -=
                MaximumExposure; // 피해 발동 후 다음 노출 주기 시작

            ApplyHazardPulse(
                hazardState.Level); // 위험도별 환경 피해 발생
        }
    }

    private void ApplyHazardPulse(
        int hazardLevel) // 노출 100% 도달 환경 피해 처리
    {
        int healthDamage =
            GetHealthDamage(
                hazardLevel); // 위험도별 체력 피해 계산

        int mentalDamage =
            GetMentalDamage(
                hazardLevel); // 위험도별 정신력 피해 계산

        BattleResultManager resultManager =
            BattleResultManager.EnsureInstance(); // 탐사 파티 영구 상태 관리자 준비

        int livingAllyCount =
            resultManager.ApplyExplorationHazardToActiveParty(
                healthDamage,
                mentalDamage); // 퇴색 피해를 현재 파티 HP·정신력에 즉시 반영

        if (livingAllyCount == 0)
        {
            sessionManager.CompleteExplorationFailure(
                "퇴색 환경 피해로 출전 파티가 전멸했습니다."); // 환경 피해 전멸 탐사 실패 처리
        }
        else if (livingAllyCount < 0)
        {
            Debug.LogWarning(
                "[Exploration][Day51] 탐사 출전 파티가 등록되지 않아 퇴색 피해를 적용하지 못했습니다."); // 파티 미등록 경고
        }

        lastHealthDamage =
            healthDamage; // 마지막 체력 피해 저장

        lastMentalDamage =
            mentalDamage; // 마지막 정신력 피해 저장

        lastPulseTime =
            Time.unscaledTime; // 마지막 피해 시각 저장

        Debug.Log(
            $"[Exploration][Day50] 퇴색 환경 피해 - " +
            $"위험도 {hazardLevel} / " +
            $"HP -{healthDamage} / " +
            $"정신 -{mentalDamage}"); // 퇴색 환경 피해 로그
    }

    private static float GetExposurePerSecond(
        int hazardLevel) // 위험도별 초당 노출량 조회
    {
        switch (hazardLevel)
        {
            case 3:
                return 25f; // 위험도 3은 약 4초마다 피해

            case 2:
                return 18f; // 위험도 2는 약 5.6초마다 피해

            default:
                return 12f; // 위험도 1은 약 8.3초마다 피해
        }
    }

    private static int GetHealthDamage(
        int hazardLevel) // 위험도별 체력 피해 조회
    {
        switch (hazardLevel)
        {
            case 3:
                return 6;

            case 2:
                return 4;

            default:
                return 2;
        }
    }

    private static int GetMentalDamage(
        int hazardLevel) // 위험도별 정신력 피해 조회
    {
        switch (hazardLevel)
        {
            case 3:
                return 3;

            case 2:
                return 2;

            default:
                return 1;
        }
    }
}
