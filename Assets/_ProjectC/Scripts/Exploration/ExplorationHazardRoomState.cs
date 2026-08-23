using UnityEngine; // Mathf 사용

public enum ExplorationHazardType // 탐사 환경 위험 종류
{
    None, // 안전 지역
    Fade // 퇴색 지역
}

public sealed class ExplorationHazardRoomState // 방 단위 환경 위험 상태
{
    public ExplorationHazardType HazardType
    {
        get;
    } // 환경 위험 종류 조회

    public int Level
    {
        get;
    } // 환경 위험도 조회

    public ExplorationHazardRoomState(
        ExplorationHazardType hazardType,
        int level) // 방 환경 위험 상태 생성
    {
        HazardType =
            hazardType; // 위험 종류 저장

        Level =
            hazardType == ExplorationHazardType.None
                ? 0
                : Mathf.Clamp(
                    level,
                    1,
                    3); // 위험 지역은 1~3 단계로 보정
    }
}
