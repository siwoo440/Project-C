using NUnit.Framework; // 실제 구역과 기존 전투의 지역 판정 비교

public sealed class ExpeditionZoneCombatRegionTests // 선택한 세계 지도 구역과 기존 지역 전투 규칙의 연결
{ // 실제 구역과 새 보정의 선택 확인 시작
    [TestCase(ExpeditionZoneId.WinterfallAshPlains, ExplorationRegion.Winterfall)] // 기존 실제 윈터폴의 동일 지역
    [TestCase(ExpeditionZoneId.FracturedEarth, ExplorationRegion.Neutral)] // 사막과 성채 밖의 암반 구역
    [TestCase(ExpeditionZoneId.CorrodedIndustrialDistrict, ExplorationRegion.Neutral)] // 기존 윈터폴 효과가 없는 산업 구역
    [TestCase(ExpeditionZoneId.FadedDunes, ExplorationRegion.Desert)] // 퇴색의 사막 실제 전투 효과 사용
    [TestCase(ExpeditionZoneId.SubterraneanVault, ExplorationRegion.Bunker)] // 실제 지하 벙커 조우의 추가 지역 값
    [TestCase(ExpeditionZoneId.RuinedCitadel, ExplorationRegion.Fortress)] // 붕괴된 성채 실제 전투 효과 사용
    [TestCase(ExpeditionZoneId.TheVoid, ExplorationRegion.Neutral)] // 기존 지역의 보정을 받지 않는 공백 구역
    public void SelectsTheCombatRegionOfTheDepartedZone(ExpeditionZoneId zone, ExplorationRegion expected) // 실제 월드맵 선택으로 지역 룰 확인
    { // 신규 실제 구역과 기본 에셋의 상호 작용 확인 시작
        Assert.AreEqual(expected, ExpeditionZoneCombatRegion.Resolve(zone, ExplorationRegion.Winterfall)); // 기존 필드가 비어도 저장한 출정 구역으로 선택
    } // 신규 실제 구역과 기본 에셋의 상호 작용 확인 종료

    [Test] // 기존 실제 원본이 분리한 조우의 호환성 확인
    public void LegacyExpeditionKeepsItsExplicitEncounterRegion() // 실제 구역이 없는 일차 이전 조우 원본 지역 보존
    { // 이전 저장의 분리 조우 확인 시작
        Assert.AreEqual(ExplorationRegion.Desert, ExpeditionZoneCombatRegion.Resolve(ExpeditionZoneId.WinterfallAshPlains, ExplorationRegion.Desert)); // 원래 명시된 실제 사막 조우의 기존 흐름
        Assert.AreEqual(0, (int)ExplorationRegion.Winterfall); // 생략하거나 비어 있는 실제 enum의 기존 기본값
        Assert.AreEqual(1, (int)ExplorationRegion.Desert); // 기존 Unity 에셋 직렬화 번호
        Assert.AreEqual(2, (int)ExplorationRegion.Fortress); // 기존 성채 자산의 실제 저장 번호
    } // 이전 저장의 분리 조우 확인 종료
} // 실제 구역과 새 보정의 선택 확인 종료
