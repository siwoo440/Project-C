using System.Linq; // 기획 구역 순서와 독립 저장 목록 비교
using NUnit.Framework; // 실제 구역 진행 규칙 검사
using UnityEngine; // 저장과 같은 JSON 직렬화 확인

public sealed class ExpeditionZoneRuntimeTests // 지역별 보스 예약과 해금 및 저장의 회귀 검사
{ // 독립 구역 검사 시작
    private ExpeditionZoneProgressSnapshot previous; // 검사로 바뀔 실제 계정 기록 보관

    [SetUp] // 각 규칙 검사의 독립 실행 시작
    public void SetUp() // 기존 계정을 보존한 새 검사 진행도 준비
    { // 검사 이전 기록 보관 시작
        previous = ExpeditionZoneProgress.Capture(); // 현재 실제 구역 진행도 스냅샷
        ExpeditionZoneProgress.Reset(); // 새 저장과 같은 초기 진행 상태
    } // 검사 이전 기록 보관 종료

    [TearDown] // 각 규칙 검사 정리
    public void TearDown() // 이전 진행도와 마지막 출전 정보 복구
    { // 검사 이전 상태 복원 시작
        ExpeditionZoneProgress.Restore(previous); // 실제 계정 상태를 시험 이전 값으로 복구
    } // 검사 이전 상태 복원 종료

    [Test] // 기획서 여덟 구역과 권장 레벨의 순서
    public void ZoneCatalogStartsWithWinterfallAndKeepsStableIdentities() // 처음 출전 가능한 하나와 고정 리소스 키 검증
    { // 원본 세계 지도 자료 확인 시작
        CollectionAssert.AreEqual(new[] { "윈터폴 잿빛 벌판", "금 간 대지", "부식된 산업 지구", "퇴색의 사막", "불타는 협곡", "지하 벙커 Δ-12", "붕괴된 성채", "공백지대" }, ExpeditionZones.All.Select(zone => zone.DisplayName)); // 여덟 구역의 정확한 표시 순서
        CollectionAssert.AreEqual(new[] { "1~5", "3~7", "5~9", "6~10", "8~12", "10~14", "12~16", "15+" }, ExpeditionZones.All.Select(zone => zone.RecommendedLevels)); // 원본 캐릭터 권장 레벨 구간 보존
        CollectionAssert.AreEqual(Enumerable.Range(0, 8), ExpeditionZones.All.Select(zone => (int)zone.Id)); // 이전 저장과 함께 고정될 타일 키의 숫자 순서
        Assert.IsTrue(ExpeditionZones.All.All(zone => zone.Id.ToString() == zone.VisualKey)); // 실제 지역 이름과 생성 원화 리소스 키 일치
        Assert.IsTrue(ExpeditionZoneProgress.IsUnlocked(ExpeditionZoneId.WinterfallAshPlains)); // 기본 윈터폴 출전 허용
        Assert.IsFalse(ExpeditionZoneProgress.IsUnlocked(ExpeditionZoneId.FracturedEarth)); // 보스 이전 영역의 출전 불가
        Assert.IsFalse(ExpeditionZoneProgress.TrySelect(ExpeditionZoneId.FracturedEarth, 1)); // 실제 출전 시 잠금 재검증
    } // 원본 세계 지도 자료 확인 종료

    [Test] // 종목별 탐사 진척과 두 손실 기록 분리
    public void ProgressCreditsOutcomesAndAdvancesOnlyTheHighestReservedExpedition() // 첫 백 퍼센트 이후 동결과 낮은 단계 재도전 검사
    { // 윈터폴 확정 결과 누적 검사 시작
        var zone = ExpeditionZoneId.WinterfallAshPlains; // 전투와 이벤트의 독립 검사 구역
        ExpeditionZoneProgress.RecordBattleVictory(zone, BattleType.Normal); // 일반 전투 승리 네 점
        ExpeditionZoneProgress.RecordBattleVictory(zone, BattleType.Elite); // 정예 전투 승리 여덟 점
        ExpeditionZoneProgress.RecordBattleVictory(zone, BattleType.Boss); // 일반 보스의 진행 점수 영점
        ExpeditionZoneProgress.RecordEvent(zone, true); // 실제 이벤트 성공 여섯 점
        ExpeditionZoneProgress.RecordEvent(zone, false); // 실제 이벤트 실패 세 점
        ExpeditionZoneProgress.RecordDefeat(zone); // 첫 전멸 한 점
        ExpeditionZoneProgress.RecordDefeat(zone); // 같은 구역의 반복 전멸 영점
        ExpeditionZoneProgress.RecordRetreat(zone); // 첫 도주 한 점
        ExpeditionZoneProgress.RecordRetreat(zone); // 같은 구역의 반복 도주 영점
        Assert.AreEqual(23, ExpeditionZoneProgress.GetPercent(zone)); // 다섯 진행 원인과 두 손실의 원본 합계
        Assert.IsFalse(ExpeditionZoneProgress.CanReserveBoss(zone, 1)); // 백 퍼센트 이전의 일반 탐사
        Fill(zone); // 이 출전 이후 예약이 생기는 첫 도달 준비
        Assert.AreEqual(100, ExpeditionZoneProgress.GetPercent(zone)); // 이후 추가 적립을 막는 백 퍼센트 고정
        Assert.IsTrue(ExpeditionZoneProgress.CanReserveBoss(zone, 1)); // 새로운 최고 레벨 출전의 보스 예약
        ExpeditionZoneProgress.RecordBossVictory(zone, 1, false); // 현재 회차에서 새로 백에 도달한 일반 보스 정산
        Assert.AreEqual(1, ExpeditionZoneProgress.GetHighestLevel(zone)); // 예약되지 않았던 현재 회차의 레벨 유지
        Assert.AreEqual(100, ExpeditionZoneProgress.GetPercent(zone)); // 기존 출발 뒤 생긴 예약 보존
        ExpeditionZoneProgress.RecordBossVictory(zone, 1, true); // 다음 실제 최고 레벨 출전에서 예약 보스 처치
        Assert.AreEqual(2, ExpeditionZoneProgress.GetHighestLevel(zone)); // 고정 한 레벨 상승
        Assert.AreEqual(0, ExpeditionZoneProgress.GetPercent(zone)); // 해당 구역의 새 진행도 사이클 시작
        ExpeditionZoneProgress.RecordDefeat(zone); // 첫 사이클에서 사용한 전멸 보정 재시도
        ExpeditionZoneProgress.RecordRetreat(zone); // 첫 사이클에서 사용한 도주 보정 재시도
        Assert.AreEqual(0, ExpeditionZoneProgress.GetPercent(zone)); // 최고 레벨 상승 뒤에도 구역당 평생 한 번 보존
        Fill(zone); // 다음 단계의 보스 예약 준비
        Assert.IsTrue(ExpeditionZoneProgress.CanReserveBoss(zone, 1)); // 낮은 해금 단계에서도 백 퍼센트 뒤 보스 재도전 허용
        ExpeditionZoneProgress.RecordBossVictory(zone, 1, true); // 낮은 재도전의 중복 승리 입력 비교
        Assert.AreEqual(2, ExpeditionZoneProgress.GetHighestLevel(zone)); // 낮은 중복 승리로 최고 단계 변화 없음
        Assert.AreEqual(100, ExpeditionZoneProgress.GetPercent(zone)); // 낮은 재도전 뒤 최고 단계 예약 보존
    } // 윈터폴 확정 결과 누적 검사 종료

    [Test] // 여덟 구역의 선행 보스 및 해당 구역 이력 검증
    public void StoryGatesRequireRealSequenceAndExplicitCitadelReport() // 식별 코드·벙커 두 이력·성채 보고의 독립 조건 확인
    { // 실제 구역 수순의 독립 검사 시작
        ExpeditionZoneProgress.RecordBossVictory(ExpeditionZoneId.WinterfallAshPlains, 1, false); // 실제 윈터폴 관문의 클리어 기록
        Assert.IsTrue(ExpeditionZoneProgress.IsUnlocked(ExpeditionZoneId.FracturedEarth)); // ‘금 간 대지’의 첫 해금
        Assert.IsFalse(ExpeditionZoneProgress.IsUnlocked(ExpeditionZoneId.CorrodedIndustrialDistrict)); // 별개 구역 진행도 전의 잠금
        Fill(ExpeditionZoneId.FracturedEarth); // 금 간 대지 최초 백 퍼센트 실제 조건
        Assert.IsTrue(ExpeditionZoneProgress.IsUnlocked(ExpeditionZoneId.CorrodedIndustrialDistrict)); // 산업 지구의 백 퍼센트 조건 확인
        ExpeditionZoneProgress.RecordBossVictory(ExpeditionZoneId.CorrodedIndustrialDistrict, 1, false); // 산업 지구 실제 관문 보스 승리
        Assert.IsTrue(ExpeditionZoneProgress.IsUnlocked(ExpeditionZoneId.FadedDunes)); // 퇴색의 사막 실제 해금
        ExpeditionZoneProgress.RecordBossVictory(ExpeditionZoneId.FadedDunes, 1, false); // 퇴색의 사막 실제 보스 승리
        Assert.IsTrue(ExpeditionZoneProgress.IsUnlocked(ExpeditionZoneId.ScorchedRavine)); // 불타는 협곡 실제 해금
        Assert.IsFalse(ExpeditionZoneProgress.HasZoneIdentificationCode); // 협곡 보스 이전의 코드 미보유
        ExpeditionZoneProgress.RecordBossVictory(ExpeditionZoneId.ScorchedRavine, 1, false); // 실제 협곡 보스 승리와 식별 코드 보상
        Assert.IsTrue(ExpeditionZoneProgress.HasZoneIdentificationCode); // 실제 협곡 정산 뒤 영구 식별 코드 기록
        Assert.IsTrue(ExpeditionZoneProgress.IsUnlocked(ExpeditionZoneId.SubterraneanVault)); // 보스와 식별 코드의 동시 충족
        Fill(ExpeditionZoneId.SubterraneanVault); // 벙커의 첫 실제 백 퍼센트 기록
        Assert.IsFalse(ExpeditionZoneProgress.IsUnlocked(ExpeditionZoneId.RuinedCitadel)); // 벙커 한 번으로는 성채 잠금 유지
        ExpeditionZoneProgress.RecordBossVictory(ExpeditionZoneId.SubterraneanVault, 1, true); // 벙커 첫 예약 공략 후 단계 다음으로 이동
        Fill(ExpeditionZoneId.SubterraneanVault); // 벙커 두 번째 실제 백 퍼센트 도달
        Assert.IsTrue(ExpeditionZoneProgress.IsUnlocked(ExpeditionZoneId.RuinedCitadel)); // 벙커 두 번째 검사에서 성채 해금
        Assert.IsFalse(ExpeditionZoneProgress.RecordMainStoryCompletion()); // 성채 승리 전 실제 보고 거절
        ExpeditionZoneProgress.RecordBossVictory(ExpeditionZoneId.RuinedCitadel, 1, false); // 성채의 실제 마지막 관문 승리
        Assert.IsTrue(ExpeditionZoneProgress.CanReportMainStory); // 최종 보고의 별도 수령 조건 확인
        Assert.IsFalse(ExpeditionZoneProgress.IsUnlocked(ExpeditionZoneId.TheVoid)); // 성채 보스 뒤에도 미보고 시 공백지대 잠금 유지
        Assert.IsTrue(ExpeditionZoneProgress.RecordMainStoryCompletion()); // 실제 출정 기록 보고의 확정
        Assert.IsTrue(ExpeditionZoneProgress.IsUnlocked(ExpeditionZoneId.TheVoid)); // 공백지대 두 조건 동시 충족
        Assert.IsFalse(ExpeditionZoneProgress.RecordMainStoryCompletion()); // 보고 중복 처리 없음 확인
    } // 실제 구역 수순의 독립 검사 종료

    [Test] // 실제 저장 자료에서 구역 진행과 출전 선택 복구
    public void ProgressRoundTripsAndLegacySelectionStartsAtWinterfall() // 선택 변경의 진입 제한 및 이전 저장의 보존 검사
    { // 선택과 진행 저장 검사 시작
        ExpeditionZoneProgress.RecordBossVictory(ExpeditionZoneId.WinterfallAshPlains, 1, false); // 선택 가능한 금 간 대지 열기
        Assert.IsFalse(ExpeditionZoneProgress.TrySelect(ExpeditionZoneId.FracturedEarth, 2)); // 해금하지 않은 구역 레벨 거부
        Assert.IsTrue(ExpeditionZoneProgress.TrySelect(ExpeditionZoneId.FracturedEarth, 1)); // 실제 출전할 구역과 단계 확인
        ExpeditionZoneProgress.RecordEvent(ExpeditionZoneId.FracturedEarth, true); // 선택한 구역의 독립 현황 준비
        string json = JsonUtility.ToJson(ExpeditionZoneProgress.Capture()); // 원래 저장과 동일한 현재 상태 직렬화
        ExpeditionZoneProgress.Restore(null); // 구역 정보가 없는 기존 거점 저장 로드
        Assert.AreEqual(ExpeditionZoneId.WinterfallAshPlains, ExpeditionZoneProgress.LastSelectedZone); // 기존 저장의 윈터폴 기본 출전 보존
        Assert.AreEqual(1, ExpeditionZoneProgress.LastSelectedLevel); // 오래된 저장의 첫 난이도 보존
        var restored = JsonUtility.FromJson<ExpeditionZoneProgressSnapshot>(json); // 원래 거점 저장의 실제 스냅샷 형식 복구
        Assert.IsTrue(ExpeditionZoneProgress.IsValidSnapshot(restored)); // 슬롯 저장의 현재 필드 검증 통과
        ExpeditionZoneProgress.Restore(restored); // 새로운 구역 슬롯 상태 저장 복원
        Assert.AreEqual(ExpeditionZoneId.FracturedEarth, ExpeditionZoneProgress.LastSelectedZone); // 실제 선택 구역 복원 확인
        Assert.AreEqual(6, ExpeditionZoneProgress.GetPercent(ExpeditionZoneId.FracturedEarth)); // 선택 구역의 이벤트 진행도 복원 확인
        Assert.AreEqual(0, ExpeditionZoneProgress.GetPercent(ExpeditionZoneId.WinterfallAshPlains)); // 다른 구역의 독립 진행도 보존
        Assert.IsTrue(ExpeditionZoneProgress.IsValidRunSelection(0, 0, 0, false)); // 기존 탐사 저장의 없는 선택 필드 허용
        Assert.IsFalse(ExpeditionZoneProgress.IsValidRunSelection(1, 100, 1, false)); // 현재 저장 구역 번호의 유효성 확인
    } // 선택과 진행 저장 검사 종료

    private static void Fill(ExpeditionZoneId zone) // 실제 정예 승리의 반복으로 백 퍼센트 도달
    { // 원래 점수 누적 시작
        for (int index = 0; index < 15; index++) // 백 퍼센트 이후 동결까지 확인하는 고정 횟수
        { // 각 실제 정예 결과 처리 시작
            ExpeditionZoneProgress.RecordBattleVictory(zone, BattleType.Elite); // 등급별 원래 정예 점수 반영
        } // 각 실제 정예 결과 처리 종료
    } // 원래 점수 누적 종료
} // 독립 구역 검사 종료
