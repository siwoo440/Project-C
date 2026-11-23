public static class ExpeditionZoneCombatRegion // 월드맵 구역 선택을 기존 지역별 전투 규칙으로 변환
{ // 호환 전투 지역의 실제 변환 시작
    public static ExplorationRegion Resolve(ExpeditionZoneId zone, ExplorationRegion authored) // 기존 조우와 실제 출정 구역으로 지역 선택
    { // 출정 구역별 기존 판정 선택 시작
        return zone switch // 실제 확정한 원정 구역의 지역 값
        { // 실제 원정 구역별 결과 시작
            ExpeditionZoneId.WinterfallAshPlains => authored, // 윈터폴 기본값 또는 이전 자산이 명시한 독립 조우 지역
            ExpeditionZoneId.FadedDunes => ExplorationRegion.Desert, // 퇴색의 사막 전용 효과와 실제 전투 연결
            ExpeditionZoneId.SubterraneanVault => ExplorationRegion.Bunker, // 지하 벙커 전용 효과와 실제 전투 연결
            ExpeditionZoneId.RuinedCitadel => ExplorationRegion.Fortress, // 붕괴된 성채 전용 효과와 실제 전투 연결
            _ => ExplorationRegion.Neutral // 정의되지 않은 원래 전투 지역 효과의 공용 보호 범위
        }; // 실제 원정 구역별 결과 종료
    } // 출정 구역별 기존 판정 선택 종료
} // 호환 전투 지역의 실제 변환 종료
