using System; // 읽기 전용 구역 목록과 고정 범위 검사
using System.Collections.Generic; // 구역 순서의 외부 읽기 전용 목록

public enum ExpeditionZoneId // 저장과 원화에서 사용하는 고정 구역 번호
{ // 공식 세계 지도 구역 번호 시작
    WinterfallAshPlains = 0, // 윈터폴 잿빛 벌판과 이전 저장 기본값
    FracturedEarth = 1, // 금 간 대지의 고정 번호
    CorrodedIndustrialDistrict = 2, // 부식된 산업 지구의 고정 번호
    FadedDunes = 3, // 퇴색의 사막의 고정 번호
    ScorchedRavine = 4, // 불타는 협곡의 고정 번호
    SubterraneanVault = 5, // 지하 벙커의 고정 번호
    RuinedCitadel = 6, // 붕괴된 성채의 고정 번호
    TheVoid = 7 // 공백지대의 고정 번호
} // 공식 세계 지도 구역 번호 종료

public sealed class ExpeditionZoneDefinition // 지도에서 보여 줄 한 구역의 공식 고정 자료
{ // 구역 기본 자료 시작
    public ExpeditionZoneId Id { get; } // 저장과 조우의 고정 식별자
    public string DisplayName { get; } // 게임 화면의 구역 이름
    public string RecommendedLevels { get; } // 권장 캐릭터 레벨 범위
    public string VisualKey => Id.ToString(); // 지도 및 타일 원화 연결값
    public string Summary { get; } // 구역의 지형과 분위기 안내
    public string RewardSummary { get; } // 기획에서 정한 주요 보상 계열
    public string UnlockDescription { get; } // 실제 다음 구역의 참여 조건

    internal ExpeditionZoneDefinition(ExpeditionZoneId id, string displayName, string recommendedLevels, string summary, string rewardSummary, string unlockDescription) // 고정된 공식 구역 자료 구성
    { // 구역 정보 대입 시작
        Id = id; // 저장에 사용하는 지역 번호
        DisplayName = displayName; // 지도 노드의 화면 이름
        RecommendedLevels = recommendedLevels; // 출전 캐릭터 권장 범위
        Summary = summary; // 지도 상세의 구역 분위기
        RewardSummary = rewardSummary; // 지도 상세의 보상 계열
        UnlockDescription = unlockDescription; // 실제 해금 조건의 안내
    } // 구역 정보 대입 종료
} // 구역 기본 자료 종료

public static class ExpeditionZones // 여덟 구역의 공식 순서와 지도 표시 자료
{ // 공식 지도 목록 시작
    public static IReadOnlyList<ExpeditionZoneDefinition> All { get; } = Array.AsReadOnly(new[] // 저장과 같은 순서의 고정 구역 읽기 목록
    { // 세계 지도 노드 구성 시작
        new ExpeditionZoneDefinition(ExpeditionZoneId.WinterfallAshPlains, "윈터폴 잿빛 벌판", "1~5", "재가 뒤덮인 윈터폴의 첫 탐사 구역.", "희귀 카드 · 금속·나사", "처음부터 탐사 가능"), // 원래 윈터폴 시작 구역
        new ExpeditionZoneDefinition(ExpeditionZoneId.FracturedEarth, "금 간 대지", "3~7", "지면이 갈라진 노출 암반과 광맥.", "희귀 카드 · 광물·철판", "윈터폴 잿빛 벌판 보스 처치"), // 두 번째 균열 지형 구역
        new ExpeditionZoneDefinition(ExpeditionZoneId.CorrodedIndustrialDistrict, "부식된 산업 지구", "5~9", "부식된 배관과 기계 설비가 남은 산업 지구.", "기계 유물 · 나사·전선", "금 간 대지 탐사 수치 100% 1회 달성"), // 세 번째 산업 구역
        new ExpeditionZoneDefinition(ExpeditionZoneId.FadedDunes, "퇴색의 사막", "6~10", "퇴색에 잠긴 모래와 무너진 구조물.", "정신력 유물 · 퇴색 관련 카드", "부식된 산업 지구 보스 처치"), // 네 번째 퇴색 모래 구역
        new ExpeditionZoneDefinition(ExpeditionZoneId.ScorchedRavine, "불타는 협곡", "8~12", "불길과 그을린 암벽이 이어진 협곡.", "공격형 카드 · 희귀 광물", "퇴색의 사막 보스 처치"), // 다섯 번째 화염 협곡 구역
        new ExpeditionZoneDefinition(ExpeditionZoneId.SubterraneanVault, "지하 벙커 Δ-12", "10~14", "잠긴 격리실과 보안 설비가 있는 지하 시설.", "전설 유물 · 특수 카드", "불타는 협곡 보스 처치 및 구역 식별 코드 확보"), // 여섯 번째 격리 벙커 구역
        new ExpeditionZoneDefinition(ExpeditionZoneId.RuinedCitadel, "붕괴된 성채", "12~16", "무너진 석벽과 마법의 흔적이 남은 성채.", "마법 계열 카드 · 기억 조각", "지하 벙커 Δ-12 탐사 수치 100% 2회 달성"), // 일곱 번째 성채 구역
        new ExpeditionZoneDefinition(ExpeditionZoneId.TheVoid, "공백지대", "15+", "공백이 드러난 마지막 탐사 구역.", "전설 유물 · 핵심 기록", "붕괴된 성채 보스 처치 및 메인 스토리 진행"), // 여덟 번째 공백 구역
    }); // 세계 지도 노드 구성 종료

    public static ExpeditionZoneDefinition Get(ExpeditionZoneId id) // 고정 구역 번호에 맞는 공식 자료 검색
    { // 구역 자료 조회 시작
        int index = (int)id; // 저장에 쓰는 번호를 목록 순서로 변환
        return index >= 0 && index < All.Count ? All[index] : null; // 등록하지 않은 번호의 빈 결과 처리
    } // 구역 자료 조회 종료
} // 공식 지도 목록 종료
