using System.Collections.Generic; // 임시 설정 값 목록 사용
using NUnit.Framework; // 튜토리얼 진행 검사 사용
public sealed class BattleTutorialProgressTests // 전투 안내 진행 규칙 검사
{ // 진행 검사 시작
    [Test] // 실제 전투 행동을 기다리는 순서 검사 지정
    public void Tutorial_TracksCardAndEnemyTurnBeforeFinishing() // 카드 사용과 적 턴을 실제로 확인하는지 검사
    { // 단계별 진행 검사 시작
        MemoryTutorialStore store = new MemoryTutorialStore(); // 독립 안내 저장소 생성
        BattleTutorialProgress tutorial = new BattleTutorialProgress(store); // 새 전투 안내 생성
        Assert.IsTrue(tutorial.StartIfNeeded(true)); // 첫 전투 자동 안내 시작 확인
        Assert.AreEqual(BattleTutorialStep.Overview, tutorial.Step); // 첫 화면 안내 확인
        tutorial.Advance(); // 카드 사용 안내 이동
        Assert.AreEqual(BattleTutorialStep.PlayCard, tutorial.Step); // 카드 사용 대기 확인
        Assert.IsFalse(tutorial.Advance()); // 카드 없이 다음 단계 차단 확인
        tutorial.OnCardUsed(); // 실제 카드 사용 전달
        Assert.AreEqual(BattleTutorialStep.ActionPoints, tutorial.Step); // 행동력 안내 이동 확인
        tutorial.Advance(); // 정신력 안내 이동
        tutorial.Advance(); // 적 예고 안내 이동
        tutorial.Advance(); // 턴 종료 안내 이동
        Assert.AreEqual(BattleTutorialStep.EndTurn, tutorial.Step); // 턴 종료 대기 확인
        tutorial.OnPhaseStarted(BattleTurnPhase.EnemyTurn); // 실제 적 턴 시작 전달
        Assert.AreEqual(BattleTutorialStep.ObserveEnemy, tutorial.Step); // 적 행동 관찰 안내 확인
        Assert.IsFalse(tutorial.Advance()); // 적 턴 진행 중 완료 차단 확인
        tutorial.OnPhaseStarted(BattleTurnPhase.PlayerTurn); // 다음 아군 턴 시작 전달
        Assert.IsTrue(tutorial.Advance()); // 적 행동 확인 후 안내 완료
        Assert.AreEqual(BattleTutorialStep.Complete, tutorial.Step); // 안내 완료 상태 확인
        Assert.AreEqual(1, store.Read("battleTutorialCompleted", 0)); // 완료 기록 저장 확인
    } // 단계별 진행 검사 종료
    [Test] // 미리 수행한 전투 행동 검사 지정
    public void Tutorial_RecognizesActionsPerformedBeforeTheirGuideStep() // 안내 전에 수행한 카드와 턴도 기록하는지 검사
    { // 미리 수행한 행동 검사 시작
        BattleTutorialProgress tutorial = new BattleTutorialProgress(new MemoryTutorialStore()); // 독립 안내 생성
        tutorial.StartIfNeeded(true); // 첫 안내 시작
        tutorial.OnCardUsed(); // 첫 안내 중 실제 카드 사용 기록
        tutorial.OnPhaseStarted(BattleTurnPhase.EnemyTurn); // 첫 안내 중 적 턴 시작 기록
        tutorial.OnPhaseStarted(BattleTurnPhase.PlayerTurn); // 첫 안내 중 다음 아군 턴 기록
        tutorial.Advance(); // 카드 사용 단계를 지나 행동력 안내 이동
        Assert.AreEqual(BattleTutorialStep.ActionPoints, tutorial.Step); // 미리 사용한 카드 반영 확인
        tutorial.Advance(); // 정신력 안내 이동
        tutorial.Advance(); // 적 예고 안내 이동
        tutorial.Advance(); // 이미 지난 턴 종료 단계를 지나 관찰 안내 이동
        Assert.AreEqual(BattleTutorialStep.ObserveEnemy, tutorial.Step); // 미리 관찰한 적 턴 반영 확인
        Assert.IsTrue(tutorial.Advance()); // 관찰 확인 후 완료 가능 확인
    } // 미리 수행한 행동 검사 종료
    [Test] // 건너뛰기와 다시 보기 검사 지정
    public void Tutorial_SkipPersistsAndReplayIsAlwaysAvailable() // 자동 안내 설정과 수동 다시 보기 검사
    { // 건너뛰기 검사 시작
        MemoryTutorialStore store = new MemoryTutorialStore(); // 독립 안내 저장소 생성
        BattleTutorialProgress tutorial = new BattleTutorialProgress(store); // 전투 안내 생성
        Assert.IsFalse(tutorial.StartIfNeeded(false)); // 설정을 끄면 자동 시작하지 않음 확인
        Assert.IsTrue(tutorial.StartIfNeeded(true)); // 설정을 켜면 미완료 안내 시작 확인
        tutorial.Skip(); // 현재 안내 건너뛰기
        Assert.AreEqual(BattleTutorialStep.Complete, tutorial.Step); // 건너뛴 안내 종료 확인
        Assert.IsFalse(new BattleTutorialProgress(store).StartIfNeeded(true)); // 새 전투에서 자동 재표시 방지 확인
        tutorial.Replay(); // 명시적인 다시 보기 시작
        Assert.AreEqual(BattleTutorialStep.Overview, tutorial.Step); // 완료 여부와 무관한 다시 보기 확인
    } // 건너뛰기 검사 종료
    private sealed class MemoryTutorialStore : ISettingsStore // 저장 없는 독립 검사 저장소
    { // 임시 저장소 시작
        private readonly Dictionary<string, int> values = new Dictionary<string, int>(); // 저장 값 보관
        public int Read(string key, int fallback) // 임시 설정 값 조회
        { // 설정 조회 시작
            return values.TryGetValue(key, out int value) ? value : fallback; // 저장 값 또는 기본값 반환
        } // 설정 조회 종료
        public void Write(string key, int value) // 임시 설정 값 기록
        { // 설정 기록 시작
            values[key] = value; // 설정 값 저장
        } // 설정 기록 종료
    } // 임시 저장소 종료
} // 진행 검사 종료
