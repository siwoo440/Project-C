using System; // 실제 테스트 유물 정리와 고정 난수 오류
using System.Collections; // 실제 플레이 모드 카드 실행 대기
using System.Linq; // 생존 아군과 정상 대상의 실제 효과 비교
using NUnit.Framework; // 전투 결과와 실제 피해 검증
using UnityEngine; // 등록 유물과 검사 카드 자료 조회
using UnityEngine.TestTools; // 진짜 플레이 모드 전환
using Fight = WinterfallSwordCombatTests.Fight; // 등록 카드와 실제 전투 환경
using Object = UnityEngine.Object; // 테스트 원본과 보관함 정리
using Random = UnityEngine.Random; // 실제 명중 전 고정 상태 난수

public sealed class WinterfallDesertBattleRelicTests // 사막 유물의 실제 전투 효과 검사
{ // 사막 전투 효과 검사 시작
    [UnityTest] // 정상 종료의 생존자 체력 저장 검사
    public IEnumerator DryWaterskinHealsLivingAlliesOnceBeforeVictoryOrEscapeCapture() // 실제 결과 스냅샷과 반복 저장 확인
    { // 승리와 도주 회복 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 종료 상태 실행
        using (var held = new OwnedRelic("DryWaterskinStrap")) // 실제 회차에서 등록된 물주머니 끈 소유
        { // 정상 종료 회복 비교 시작
            CheckWaterskin(BattleResult.Escape); // 실제 도주 완료값의 회복 검사
            CheckWaterskin(BattleResult.Victory); // 실제 마지막 적 처치 승리 회복 검사
        } // 정상 종료 회복 비교 종료
        yield return new ExitPlayMode(); // 격리 전투 종료
    } // 승리와 도주 회복 검사 종료

    private static void CheckWaterskin(BattleResult expected) // 각각 실제 정상 종료한 파티의 체력 저장
    { // 실제 부상 아군과 전투불능 아군 준비 시작
        using var battle = new Fight("RUNE_BURST"); // 생존 적과 아군의 정상 조우
        var wounded = battle.Allies[0]; // 회복이 필요한 첫 생존 아군
        var nearlyFull = battle.Allies[1]; // 원래 최대 체력까지만 회복할 동료
        var fallen = battle.Allies[2]; // 유물 회복의 대상이 아닌 실제 전투불능 동료
        wounded.TakeDamage(7, BattleDamageType.None); // 충분한 실제 회복 여유 생성
        nearlyFull.TakeDamage(1, BattleDamageType.None); // 한 점의 실제 회복 여유 생성
        fallen.TakeDamage(fallen.CurrentHealth, BattleDamageType.None); // 전투불능 유닛의 정상 처리
        int before = wounded.CurrentHealth; // 전투 종료 회복 전 실제 체력
        Assert.AreEqual(before, battle.Turns.CaptureBattleResult(Array.Empty<string>()).AllyStates[0].CurrentHealth); // 결과가 없는 전투에서는 회복 미리 지급하지 않음
        if (expected == BattleResult.Escape) // 일반 조우 실제 도주 여부
        { // 도주 상태 확정 시작
            Assert.IsTrue(battle.Turns.TryEscape()); // 정상 전투 도주 실행
        } // 도주 상태 확정 종료
        else // 실제 마지막 적 처치 경로
        { // 전투 승리 확정 시작
            foreach (var enemy in battle.Enemies) // 정상 전투에 등록된 적 순회
            { // 실제 마지막 적까지 제거 시작
                enemy.TakeDamage(enemy.CurrentHealth, BattleDamageType.None); // 실제 적 사망 사건 발행
            } // 실제 마지막 적까지 제거 종료
        } // 전투 승리 확정 종료
        Assert.AreEqual(expected, battle.Turns.Result); // 스냅샷 생성 전 정상 종료 단계 확인
        var saved = battle.Turns.CaptureBattleResult(Array.Empty<string>()); // 실제 씬에서 사용하는 결과 저장 원본
        Assert.AreEqual(before + 2, saved.AllyStates[0].CurrentHealth); // 정상 종료 전에 실제 회복한 두 점 저장
        Assert.AreEqual(nearlyFull.MaxHealth, saved.AllyStates[1].CurrentHealth); // 원래 최대 체력의 실제 회복 상한
        Assert.IsTrue(saved.AllyStates[2].IsDead); // 전투불능 인원을 치료로 되살리지 않는 확인
        Assert.AreEqual(saved.AllyStates[0].CurrentHealth, battle.Turns.CaptureBattleResult(Array.Empty<string>()).AllyStates[0].CurrentHealth); // 같은 결과 저장 재시도에서 회복 한 번 유지
    } // 실제 부상 아군과 전투불능 아군 준비 종료

    [UnityTest] // 유효한 첫 직접 공격 전후의 모래 뼛조각 검증
    public IEnumerator SandBonePreviewStaysAvailableThroughMissAndNullificationUntilARealHit() // 직접 양수 명중과 전투당 소모 조건 검증
    { // 출혈 대상의 실제 명중 검사 시작
        yield return new EnterPlayMode(); // 손패 및 적 상태가 연결된 실제 전투 시작
        using (var held = new OwnedRelic("SandStainedBoneFragment")) // 두 실제 연속 전투에서 같은 정식 뼛조각 보유
        { // 같은 유물을 공유한 전투 검사 시작
            yield return CheckSandBone(); // 두 전투의 실제 피해 가산 및 초기화 확인
        } // 같은 유물을 공유한 전투 검사 종료
        yield return new ExitPlayMode(); // 격리 전투 종료
    } // 출혈 대상의 실제 명중 검사 종료

    private static IEnumerator CheckSandBone() // 현재와 새 전투의 실제 첫 명중 대조
    { // 전투당 뼛조각 규칙 확인 시작
        using (var battle = new Fight("RUNE_BURST", "RUNE_BURST", "RUNE_BURST", "RUNE_BURST")) // 같은 실제 직접 피해 원본 네 장
        { // 빗나감과 보호 및 막힌 타격 검사 시작
            var source = battle.Allies[0]; // 실제 원본 공격 카드 소유자
            var target = battle.Enemies[0]; // 타격 전에 실제 출혈 중인 생존 적
            target.ApplyStatusEffect(BattleStatusEffectType.Bleed, 1, 4, 1, source); // 유효 범위의 활성 출혈 상태 부여
            target.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 2, 1); // 첫 원본 공격의 확정 회피
            Assert.AreEqual(13, target.PreviewDamage(11, BattleDamageType.Magical, source, alliedDirectAttack: true).RawDamage); // 실제 방어 이전 공유 예고에서 아직 남은 추가 피해
            yield return battle.Use("RUNE_BURST", 0); // 출혈 적이 실제로 피하는 첫 카드
            Assert.AreEqual(200, target.CurrentHealth); // 빗나감의 실제 피해 없음
            Assert.AreEqual(13, target.PreviewDamage(11, BattleDamageType.Magical, source, alliedDirectAttack: true).RawDamage); // 빗나간 뒤에도 첫 유효 피해 가산의 예고 유지
            target.RemoveStatusEffect(BattleStatusEffectType.Evasion); // 정상 실제 명중 가능한 상태 복원
            target.ApplyStatusEffect(BattleStatusEffectType.DamageNullification, 1, 2, 1); // 양수 피해를 한 번 무효 처리
            yield return battle.Use("RUNE_BURST", 0); // 실제 명중했지만 전부 무효화되는 카드
            Assert.AreEqual(200, target.CurrentHealth); // 원래 피해 무효의 실제 발동 확인
            Assert.AreEqual(13, target.PreviewDamage(11, BattleDamageType.Magical, source, alliedDirectAttack: true).RawDamage); // 피해 무효 뒤에도 남아 있는 같은 실제 기회
            target.GainBlock(30); // 다음 공격에서 방어도만 실제 잃는 비교 처리
            yield return battle.Use("RUNE_BURST", 0); // 활성 출혈 적에 대한 최초 유효 직접 양수 타격
            Assert.AreEqual(17, target.CurrentBlock); // 기본 11과 가산 2가 실제 방어도를 소모
            Assert.AreEqual(11, target.PreviewDamage(11, BattleDamageType.Magical, source, alliedDirectAttack: true).RawDamage); // 실제 명중 소모 후 같은 예고 가산 없음
            yield return battle.Use("RUNE_BURST", 0); // 같은 대상의 다음 직접 카드 명중
            Assert.AreEqual(6, target.CurrentBlock); // 전투당 한 번 뒤 원본 피해만 적용
        } // 빗나감과 보호 및 막힌 타격 검사 종료
        using (var battle = new Fight("RUNE_BURST")) // 같은 원본을 가진 독립 다음 전투
        { // 새 전투의 가산 초기화 확인 시작
            var source = battle.Allies[0]; // 새 전투의 원래 시전자
            var target = battle.Enemies[0]; // 새 전투의 유효 출혈 대상
            target.ApplyStatusEffect(BattleStatusEffectType.Bleed, 1, 4, 1, source); // 다음 전투의 새 활성 출혈
            yield return battle.Use("RUNE_BURST", 0); // 새 전투의 첫 살아 있는 적 명중
            Assert.AreEqual(187, target.CurrentHealth); // 전투당 새 첫 기회로 늘어난 실제 체력 피해
        } // 새 전투의 가산 초기화 확인 종료
    } // 전투당 뼛조각 규칙 확인 종료

    [UnityTest] // 확률 실명과 시작 공격 감소의 실제 게임 경로 검사
    public IEnumerator BlackSandChangesOptionalBlindAndBraceletWeakensExactlyOneEnemyBeforeItsFirstTurn() // 기존 상태 처리의 실제 전투 진입과 확률 경계 확인
    { // 사막 실명과 개막 약화 검사 시작
        yield return new EnterPlayMode(); // 전투 개막과 실제 손패 충돌 활성화
        using (var held = new OwnedRelic("BlackSandVial")) // 플레이 모드 전체에서 같은 실제 유물 보관함 유지
        { // 같은 회차의 실명과 개막 효과 검사 시작
            yield return CheckBlackSand(); // 실제 아군 카드로 선택적 실명 경계 사용
            using var bracelet = new OwnedRelic("DesertKingBrokenBracelet"); // 등록 사막왕 팔찌의 실제 보유
            CheckBracelet(); // 등록 유물의 실제 개막 약화와 두 턴 확인
        } // 같은 회차의 실명과 개막 효과 검사 종료
        yield return new ExitPlayMode(); // 실제 격리 전투 종료
    } // 사막 실명과 개막 약화 검사 종료

    private static void CheckBracelet() // 실제 적의 개막 공격력과 수명 확인
    { // 현재 전투의 한 명 초기 약화 검사 시작
        using var battle = new Fight("RUNE_BURST"); // 네 생존 적의 실제 시작 전투
        var chosen = battle.Enemies.Single(enemy => enemy.GetStatusEffect(BattleStatusEffectType.AttackPowerDown) != null); // 첫 턴 전에 적용된 유일한 생존 적
        Assert.AreEqual(2, chosen.GetStatusEffect(BattleStatusEffectType.AttackPowerDown)?.EffectiveValue); // 등록된 공격력 감소 두 점
        Assert.AreEqual(2, chosen.GetStatusEffect(BattleStatusEffectType.AttackPowerDown)?.RemainingTurns); // 시작 때 남은 두 실제 적 턴
        Assert.IsTrue(battle.Turns.EndPlayerTurn()); // 첫 적 턴 실제 시작
        Assert.IsTrue(battle.Turns.CompleteEnemyTurn()); // 첫 적 턴 실제 종료
        Assert.AreEqual(1, chosen.GetStatusEffect(BattleStatusEffectType.AttackPowerDown)?.RemainingTurns); // 정확히 한 적 턴 남은 약화
        Assert.IsTrue(battle.Turns.EndPlayerTurn()); // 두 번째 적 차례 시작
        Assert.IsTrue(battle.Turns.CompleteEnemyTurn()); // 두 번째 적 차례 실제 종료
        Assert.IsNull(chosen.GetStatusEffect(BattleStatusEffectType.AttackPowerDown)); // 기존 상태 수명으로 실제 두 적 턴 뒤 해제
    } // 현재 전투의 한 명 초기 약화 검사 종료

    private static IEnumerator CheckBlackSand() // 원래 선택 실명의 실제 확률과 보장 효과 비교
    { // 실명 원본과 보유 유물 준비 시작
        var optional = CreateBlind(30); // 원래 삼십 퍼센트의 실제 선택 실명 원본
        var guaranteed = CreateBlind(100); // 원래 확정인 분리된 실제 실명 원본
        try // 실제 카드 검사와 원본 해제 보장
        { // 같은 카드의 확률 경계 시작
            using var battle = new Fight(new[] { optional, optional, guaranteed }); // 실제 원본을 사용하는 손패와 적 준비
            int seed = SeedFor(34); // 증가한 확률에서의 마지막 성공 난수
            battle.Controller.CardUsed += (card, targets) => Random.InitState(seed); // 실제 카드 충돌 직전에 확률 재현
            yield return WinterfallCommonCardExpansionTests.Play(optional, battle.Hand, battle.EnemyViews[0], battle.Runner, battle.Deck, battle.Points); // 마지막 성공 난수의 실제 카드 실행
            Assert.IsNotNull(battle.Enemies[0].GetStatusEffect(BattleStatusEffectType.Blind)); // 원래 삼십을 넘어도 실제 추가 오 퍼센트 적용 성공
            seed = SeedFor(35); // 증가한 확률 다음의 첫 실패 난수
            yield return WinterfallCommonCardExpansionTests.Play(optional, battle.Hand, battle.EnemyViews[1], battle.Runner, battle.Deck, battle.Points); // 증가 경계에서 첫 실패의 실제 카드 사용
            Assert.IsNull(battle.Enemies[1].GetStatusEffect(BattleStatusEffectType.Blind)); // 삼십오에서는 실제 증가 확률 실패
            yield return WinterfallCommonCardExpansionTests.Play(guaranteed, battle.Hand, battle.EnemyViews[2], battle.Runner, battle.Deck, battle.Points); // 원래 백 퍼센트 효과 실제 사용
            Assert.IsNotNull(battle.Enemies[2].GetStatusEffect(BattleStatusEffectType.Blind)); // 원래 확정 상태는 그대로 실제 부여
            Assert.AreEqual(35, Random.Range(0, 100)); // 원래 확정 효과의 추가 난수 미소모
        } // 같은 카드의 확률 경계 종료
        finally // 검사 원본의 안전한 정리
        { // 검사 카드 제거 시작
            Object.Destroy(optional); // 실제 확률형 실명 검사용 카드 제거
            Object.Destroy(guaranteed); // 실제 보장 실명 검사용 카드 제거
        } // 검사 카드 제거 종료
    } // 실명 원본과 보유 유물 준비 종료

    private static CardData CreateBlind(int chance) // 기존 원본 선택 실명 필드를 가진 실제 검사 카드
    { // 실제 전투 데이터 원본 생성 시작
        var card = ScriptableObject.CreateInstance<CardData>(); // 실제 Unity 카드 원본 생성
        JsonUtility.FromJsonOverwrite($"{{\"cardId\":\"DESERT_BLIND_{chance}\",\"targetType\":{(int)CardTargetType.SingleEnemy},\"effectType\":{(int)CardEffectType.ApplyStatusEffect},\"statusEffectType\":{(int)BattleStatusEffectType.Blind},\"effectValue\":20,\"optionalBlindChancePercent\":{chance}}}", card); // 실제 전투에서 사용할 명시 확률 실명 자료 입력
        return card; // 실제 손패에 넣을 원본 반환
    } // 실제 전투 데이터 원본 생성 종료

    private static int SeedFor(int roll) // 지정 경계 숫자가 나오는 실제 Unity 시드 찾기
    { // 재현 가능한 후보 난수 검사 시작
        for (int seed = 0; seed < 10000; seed++) // 정해진 유효 시드 후보 순회
        { // 현재 시드 난수 확인 시작
            Random.InitState(seed); // 실제 Unity 난수 발생기의 후보 시작점 설정
            if (Random.Range(0, 100) == roll) // 요청한 실제 경계와 같은 후보 확인
            { // 재현할 원본 시드 선택 시작
                return seed; // 카드 사용 직전에 사용할 후보 값
            } // 재현할 원본 시드 선택 종료
        } // 현재 시드 난수 확인 종료
        throw new InvalidOperationException("Expected Blind roll not found."); // 정해진 후보에서 재현 난수 누락
    } // 재현 가능한 후보 난수 검사 종료

    private sealed class OwnedRelic : IDisposable // 실제 회차 유물의 독립 획득과 정리
    { // 보유 유물 범위 시작
        private readonly RelicRunManager previous = RelicRunManager.Instance; // 검사 이전 실제 관리자 참조
        private readonly RelicRunManager manager = RelicRunManager.EnsureInstance(); // 실제 회차 유물 보관함 연결
        private readonly RelicData relic; // 이 검사에서 보유한 실제 원본 자료

        internal OwnedRelic(string name) // 정식 등록 유물의 원본 이름 전달
        { // 실제 보유 항목 추가 시작
            relic = Resources.Load<RelicData>("Relics/" + name); // 정상 유물 리소스에서 실제 원본 조회
            Assert.IsNotNull(relic, name); // 실제 등록된 유물 자료 확인
            Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(relic)); // 정상 회차 보유 경로의 실제 획득 확인
        } // 실제 보유 항목 추가 종료

        public void Dispose() // 테스트에서만 획득한 실제 유물 제거
        { // 보유 유물 정리 시작
            manager.TryRemove(relic.RelicId); // 현재 검사 원본의 유물 보유 해제
            if (previous == null) // 이 검사용으로 실제 관리자를 만든 경우
            { // 임시 관리자 제거 시작
                Object.Destroy(manager.gameObject); // 이미 정리한 회차 보관함 객체 해제
            } // 임시 관리자 제거 종료
        } // 보유 유물 정리 종료
    } // 보유 유물 범위 종료
} // 사막 전투 효과 검사 종료
