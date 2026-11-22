using System.Collections; // 실제 플레이 모드 전환과 카드 처리 프레임
using System.Linq; // 아군 방어도와 실제 결과 합계 조회
using NUnit.Framework; // 차례와 독립 상태 변화의 실제 검사
using UnityEngine; // 정식 보유 유물의 원본 에셋 조회
using UnityEngine.TestTools; // 실제 검증용 Unity 플레이 모드
using Fight = WinterfallSwordCombatTests.Fight; // 기존 카드와 정상 전투의 공통 검사 환경

public sealed class WinterfallRecoveryRelicTests // 폐기장 왕관과 안전등 및 용암 포자의 실제 전투 회귀
{ // 원래 조우 경계의 유물 검사 시작
    [UnityTest] // 직접 공격 외의 출처 없는 실제 적 전투불능과 게임 종료 순서
    public IEnumerator CrownGrantsEachRegisteredDefeatBeforeFinalResult() // 감전 사망과 소환 해제 및 마지막 전투불능 구분
    { // 원래 Unity 플레이 모드의 실제 전투 진입
        yield return new EnterPlayMode(); // 정식 승인된 유물과 원본 카드의 유니티 환경
        Acquire("ScrapyardCrown"); // 효과 값 여섯인 실제 배포 왕관 원본
        using (var fight = new Fight("THRUST")) // 네 실제 적과 여러 아군이 있는 공통 전투
        { // 실제 적 패배와 제거 경계 검사 시작
            var fallen = fight.Allies[2]; // 무작위 방어도 대상에서 제외될 아군
            fallen.TakeDamage(fallen.MaxHealth * 3, BattleDamageType.None); // 정상 실제 피해로 쓰러진 아군 준비
            Assert.IsTrue(fallen.IsDead); // 아군 사망에서는 방어 지급이 없는 상태 확인
            Assert.AreEqual(0, fight.Allies.Sum(ally => ally.CurrentBlock)); // 아군 전투불능은 적 처치 효과를 발동하지 않음
            var shocked = fight.Enemies[0]; // 출처 없는 원래 지속 피해로 죽을 적
            shocked.ApplyStatusEffect(BattleStatusEffectType.Shock, shocked.MaxHealth * 3, 1, 1); // 실제 적 턴의 일반 감전으로 생존 체력 이상 감소
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 게임의 실제 적 턴 시작과 감전 작동
            Assert.IsTrue(shocked.IsDead); // 일반 감전 경로에서 실제 사망했는지 확인
            Assert.AreEqual(6, fight.Allies.Sum(ally => ally.CurrentBlock)); // 실제 출처 없는 적 전투불능에서 한 명에게 여섯 지급
            Assert.AreEqual(1, fight.Allies.Count(ally => ally.CurrentBlock > 0)); // 현재 생존 파티에서 실제 한 명만 지급 확인
            shocked.TakeDamage(shocked.MaxHealth * 3, BattleDamageType.None); // 이미 사망한 같은 적에게 반복 피해 입력
            Assert.AreEqual(6, fight.Allies.Sum(ally => ally.CurrentBlock)); // 이미 정산된 동일 적 사망의 중복 보상 없음
            var removed = fight.Enemies[1]; // 이번 전투의 실제 이벤트를 해제할 비소환 남은 적
            Assert.IsTrue(fight.Turns.UnregisterEnemy(removed)); // 원래 소환 개체를 제거할 때 쓰는 실제 구독 해제
            Assert.AreEqual(6, fight.Allies.Sum(ally => ally.CurrentBlock)); // 피해 없는 개체 이탈 자체의 무발동 확인
            removed.TakeDamage(removed.MaxHealth * 3, BattleDamageType.None); // 이전 등록 개체의 실제 전투 밖 퇴장
            Assert.AreEqual(6, fight.Allies.Sum(ally => ally.CurrentBlock)); // 제거된 개체의 이후 전투불능에서 혜택 없음
            fight.Enemies[2].TakeDamage(fight.Enemies[2].MaxHealth * 3, BattleDamageType.None, fight.Allies[0]); // 실제 살아 있는 아군 출처로 다른 적 정산
            Assert.AreEqual(12, fight.Allies.Sum(ally => ally.CurrentBlock)); // 둘째 적도 독립 방어도 여섯 지급
            int atResult = -1; // 실제 전투 결과 발행 시점의 방어도
            fight.Turns.BattleCompleted += _ => atResult = fight.Allies.Sum(ally => ally.CurrentBlock); // 종료 화면 전에 마지막 보상이 지급됐는지 실제 순서 기록
            fight.Enemies[3].TakeDamage(fight.Enemies[3].MaxHealth * 3, BattleDamageType.None); // 마지막 남은 적의 실제 전투불능
            Assert.AreEqual(BattleResult.Victory, fight.Turns.Result); // 제거 상태와 다른 실제 마지막 처치로 승리 확인
            Assert.AreEqual(18, atResult); // 승리 결과 이전 세 등록 적의 실제 방어 발동 반영
            Assert.AreEqual(0, fallen.CurrentBlock); // 이미 쓰러진 아군은 끝까지 무작위 영입 대상에서 제외
        } // 실제 적 패배와 제거 경계 검사 종료
        yield return new ExitPlayMode(); // 실제 상태와 아군 유물 검사 환경 종료
    } // 원래 Unity 플레이 모드의 실제 전투 검사 종료

    [UnityTest] // 정신력 0에 실제 도착한 살아 있는 팀의 첫 사례만 안전등 작동
    public IEnumerator SafetyLampRecoversActualFirstZeroOnceForWholeBattle() // 미리 보기와 초기 저장값 및 팀 전체 한 번 조건
    { // 실제 정신력 변화 경계 검사 시작
        yield return new EnterPlayMode(); // 정식 안전등과 원래 전투를 함께 실행
        Acquire("LastSafetyLamp"); // 출시 원본의 정신력 열 점 회복
        using (var fight = new Fight("THRUST")) // 별개 아군의 실제 정신 상태 확인 준비
        { // 원본 현재 정신력과 실제 결과 비교 시작
            var first = fight.Allies[0]; // 안전등이 처음 도울 실제 살아 있는 아군
            var other = fight.Allies[1]; // 같은 조우의 이미 쓴 안전등을 확인할 다음 아군
            var loaded = fight.Allies[2]; // 저장 정신력이 영이어도 실제 피해가 없었던 비교 아군
            Assert.IsTrue(loaded.ApplyPersistentMental(0)); // 저장 원본의 영 초기화가 비용을 소비하지 않음을 확인
            Assert.AreEqual(0, loaded.CurrentMental); // 실제 손실이 없는 초기 영 값의 유지
            Assert.IsTrue(first.ApplyPersistentMental(4)); // 실제 감소 후 남은 양수와 영을 차례대로 만들 기준
            Assert.AreEqual(-4, first.PreviewMentalChange(-100)); // 원래 영점 예고만 읽고 실제 상태 유지
            Assert.AreEqual(4, first.CurrentMental); // 미리 보기에서 원래 정신력과 유물 기회 보존
            first.ChangeMental(-3, BattleMentalChangeReason.CardEffect); // 아직 양수로 남는 실제 카드 정신 손실
            Assert.AreEqual(1, first.CurrentMental); // 영에 닿지 않은 실제 감소에서 미발동
            var loss = first.ChangeMental(-5, BattleMentalChangeReason.CardEffect); // 실제 정신력이 한 점 줄어 영에 닿는 원래 사건
            Assert.AreEqual(-1, loss.AppliedDelta); // 원래 정신 피해의 실제 적용 손실을 변조하지 않음
            Assert.AreEqual(0, loss.CurrentMental); // 원래 피해는 실제 영 도착 결과 유지
            Assert.AreEqual(10, first.CurrentMental); // 결과 직후 원래 회복 경로의 정신력 열 점 증가
            Assert.AreEqual(BattleMentalState.Collapse, first.MentalState); // 영에 도달한 기존 붕괴를 보조 효과가 해제하지 않음
            Assert.IsTrue(other.ApplyPersistentMental(2)); // 다른 건강한 아군의 동일 영 손실 재현
            other.ChangeMental(-2, BattleMentalChangeReason.DisruptReceived); // 같은 조우의 다른 실제 영 도착
            Assert.AreEqual(0, other.CurrentMental); // 이미 쓴 안전등은 다른 팀원에게 재발동하지 않음
            first.ChangeMental(-10, BattleMentalChangeReason.RelicEffect); // 회복받은 유닛의 두 번째 실제 영점
            Assert.AreEqual(0, first.CurrentMental); // 원래 회복 수신자도 같은 전투에서 다시 도와주지 않음
        } // 원본 현재 정신력과 실제 결과 비교 종료
        yield return new ExitPlayMode(); // 실제 정신력 검사 종료
    } // 실제 정신력 변화 경계 검사 종료

    [UnityTest] // 일반과 동전 시간으로 세 번째 아군 시작을 모두 구분
    public IEnumerator SporeHealsOneWoundedAllyInRosterOrderOnThirdRealTurn() // 첫 두 차례 및 온전 체력인 낮은 상한 유닛 제외
    { // 실제 아군 턴과 추가 시간의 원래 흐름 확인 시작
        yield return new EnterPlayMode(); // 원래 덱과 한 번 치유하는 유물의 실제 환경
        Acquire("LavaMossSporeBottle"); // 세 아군 턴의 원래 포자병 획득
        using (var fight = new Fight("TIME_COIN", "THRUST")) // 정상 손패의 구체적인 동전 카드로 추가 아군 차례 준비
        { // 실제 부상자 세기와 세 번째 차례 검증 시작
            var first = fight.Allies[0]; // 동률 체력에서 먼저 선택할 원래 배치 아군
            var second = fight.Allies[1]; // 체력이 같은 다음 배치 아군
            var full = fight.Allies[2]; // 얼굴 수치가 더 낮지만 건강한 비교 유닛
            Assert.IsTrue(first.ApplyPersistentHealth(20) && second.ApplyPersistentHealth(20)); // 양쪽의 같은 현재 체력과 실제 부상 상태 준비
            full.ModifyMaxHealth(1 - full.MaxHealth); // 현재 수치가 낮은 건강한 유닛을 실제 치유 후보에서 제외해야 할 상황
            Assert.AreEqual(1, full.CurrentHealth); // 원래 상한과 현재 체력이 동등한 대상 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 첫 정상 적 차례를 통과해 실제 두 번째 아군 시작
            Assert.AreEqual(2, fight.Turns.CurrentRound); // 이번 실제 아군 차례 번호 확인
            Assert.AreEqual(20, first.CurrentHealth); // 앞선 두 아군 실제 차례에는 회복하지 않음
            yield return fight.Use("TIME_COIN"); // 실제 손패의 시간 동전 사용으로 다음 추가 시간 예약
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 추가 아군 차례 시작
            Assert.AreEqual(3, fight.Turns.CurrentRound); // 시간 동전으로 시작한 차례도 셋째로 집계
            Assert.AreEqual(BattleTurnPhase.PlayerTurn, fight.Turns.CurrentPhase); // 적이 행동하기 전 실제 하나의 아군 차례 확인
            Assert.AreEqual(24, first.CurrentHealth); // 같은 체력에서 앞선 아군이 일반 회복 네 점을 받음
            Assert.AreEqual(20, second.CurrentHealth); // 같은 현재 체력의 뒤 배치 아군은 이번에 회복하지 않음
            Assert.AreEqual(1, full.CurrentHealth); // 실제 부상이 없는 아군은 자기 원래 수치 유지
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 이후 정상 네 번째 아군 시작을 실제 실행
            Assert.AreEqual(24, first.CurrentHealth); // 다음 일반 아군 단계에서 같은 주기를 다시 쓰지 않음
            Assert.AreEqual(20, second.CurrentHealth); // 셋째 차례의 단일 회복에만 한정
        } // 실제 부상자 세기와 세 번째 차례 검증 종료
        yield return new ExitPlayMode(); // 원래 수명 검사의 플레이 모드 종료
    } // 실제 아군 턴과 추가 시간의 원래 흐름 확인 종료

    private static void Acquire(string resourceName) // 테스트가 지정한 실제 출시에 포함된 유물 보유
    { // 정식 보유 에셋 확인 시작
        var data = Resources.Load<RelicData>("Relics/" + resourceName); // 게임 원본 가져오기 경로에서 정식 유물 로드
        Assert.IsNotNull(data, resourceName); // 실제 시트 원본이 준비되었는지 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(data)); // 실제 현재 회차의 정상 보유로 등록
    } // 정식 보유 에셋 확인 종료
} // 원래 조우 경계의 유물 검사 종료
