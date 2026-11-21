using System.Collections; // 실제 카드 연출과 모드 전환 대기
using NUnit.Framework; // 지팡이 피해와 행동력 결과 검증
using UnityEngine; // 전투 시간과 화면 조회
using UnityEngine.TestTools; // 실제 전투 검사 모드 전환
using Fight = WinterfallSwordCombatTests.Fight; // 원본 카드와 손패 진입점 재사용
using Random = UnityEngine.Random; // 실제 전투 확률의 경계 시드 설정

public sealed class WinterfallManaConcentrationAndSiphonTests // 집중과 흡수의 실제 카드 사용 경계
{ // 마력 카드 회귀 검사 시작
    [UnityTest] // 턴과 여러 적에 걸친 집중의 실제 카드 사용 검사
    public IEnumerator ConcentrationRefreshesAndBoostsEachLandedTargetOfOneWandCard() // 하나의 실제 지팡이 카드 사용으로 모두 소모 확인
    { // 집중 카드 플레이 모드 검사 시작
        yield return new EnterPlayMode(); // 실제 손패와 카드 연출이 동작하는 모드 진입
        yield return VerifyConcentration(); // 갱신과 여러 턴 유지 및 대상별 타격 확인
        yield return new ExitPlayMode(); // 독립 전투 정리 뒤 편집 모드 복귀
    } // 집중 카드 플레이 모드 검사 종료

    private static IEnumerator VerifyConcentration() // 집중이 붙고 소모되는 일반 입력과 명중 경로
    { // 집중 카드 동작 확인 시작
        using var fight = new Fight("MANA_CONCENTRATION", "MANA_CONCENTRATION", "LIGHTNING_STORM", "COLD_TOUCH", "CHARGE"); // 두 집중과 여러 계열 피해를 포함한 실제 카드 덱
        var owner = fight.Allies[0]; // 집중 상태를 얻을 카드의 실제 소유자
        Assert.IsTrue(owner.ApplyPersistentMental(50)); // 정신력 특수 상태가 없는 일반 피해 조건 준비
        Assert.AreEqual(CardTargetType.Self, fight.View("MANA_CONCENTRATION").RuntimeCard.TargetType); // 원본 카드의 소유자 대상 확인
        Assert.Zero(fight.View("MANA_CONCENTRATION").RuntimeCard.ApCost); // 원본 카드의 행동력 영 비용 확인
        yield return fight.Use("MANA_CONCENTRATION"); // 실제 손패의 첫 집중 카드 사용
        yield return fight.Use("MANA_CONCENTRATION"); // 같은 시전자에게 집중 한 번 갱신
        var effect = owner.GetStatusEffect(BattleStatusEffectType.ManaConcentration); // 실제 상태 목록에서 집중 조회
        Assert.IsNotNull(effect); // 화면에 표시하는 공용 상태 효과 생성 확인
        Assert.AreEqual(4, effect.EffectiveValue); // 두 번째 시전 뒤에도 네 점의 원본 수치 확인
        Assert.AreEqual(1, effect.StackCount); // 실제 집중 상태의 한 중첩 확인
        var candidate = fight.View("COLD_TOUCH"); // 아직 공격을 사용하지 않은 지팡이 카드
        candidate.OnPointerClick(null); // 일반 손패 입력의 적 지정 대기
        Assert.AreSame(candidate.RuntimeCard, fight.Controller.SelectedCard); // 실제 공격을 기다리는 상태 확인
        candidate.OnPointerClick(null); // 같은 카드 재선택의 정상 취소
        Assert.IsNull(fight.Controller.SelectedCard); // 카드 지정을 취소한 상태 확인
        Assert.AreSame(effect, owner.GetStatusEffect(BattleStatusEffectType.ManaConcentration)); // 대상만 고른 상태에서는 기존 집중 유지 확인
        yield return fight.Use("CHARGE", 3); // 같은 소유자의 검 피해 카드 실제 사용
        Assert.AreSame(effect, owner.GetStatusEffect(BattleStatusEffectType.ManaConcentration)); // 검 계열 피해를 줘도 집중 유지 확인
        for (int turn = 0; turn < 3; turn++) // 한 턴 한도를 초과하는 실제 전투 턴 전환 반복
        { // 개별 지속 턴 확인 시작
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 기존 적 턴 시작 처리
            Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 다음 아군 턴 준비와 드로우
            Assert.AreSame(effect, owner.GetStatusEffect(BattleStatusEffectType.ManaConcentration)); // 여러 턴을 지나도 사용 전까지 같은 집중 상태 확인
        } // 개별 지속 턴 확인 종료
        fight.Enemies[1].ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 1, 1); // 광역 공격의 두 번째 대상만 확정 회피 설정
        fight.Enemies[3].TakeDamage(1000, BattleDamageType.None); // 실제 피해로 대상 목록에서 제외할 적 처치
        int firstDamage = -1; // 광역 카드의 첫 실제 피해 원본 보관
        int anotherDamage = -1; // 동일 카드의 다음 유효 타격 원본 보관
        fight.Enemies[0].DamageTaken += (_, result) => firstDamage = result.RawDamage; // 첫 대상의 실제 타격 피해 수집
        fight.Enemies[2].DamageTaken += (_, result) => anotherDamage = result.RawDamage; // 세 번째 대상의 실제 타격 피해 수집
        yield return fight.Use("LIGHTNING_STORM"); // 생존 적 전체를 대상으로 삼는 원본 지팡이 공격
        Assert.AreEqual(12, firstDamage); // 기본 마법 피해 여덟과 집중 네 점의 첫 명중 합계 확인
        Assert.AreEqual(12, anotherDamage); // 중간 빗나감 뒤 같은 카드의 다음 명중에도 네 점 확인
        Assert.AreEqual(fight.Enemies[1].MaxHealth, fight.Enemies[1].CurrentHealth); // 빗나간 광역 대상의 체력 보존 확인
        Assert.IsNull(owner.GetStatusEffect(BattleStatusEffectType.ManaConcentration)); // 한 장의 지팡이 공격에 사용된 집중 상태 제거 확인
        yield return fight.Use("COLD_TOUCH", 0); // 두 번째 지팡이 피해를 같은 적에게 실제 사용
        Assert.AreEqual(5, firstDamage); // 이후 지팡이 공격에 집중이 이어지지 않는지 확인
    } // 집중 카드 동작 확인 종료

    [UnityTest] // 타격, 빗나감, 잘못된 선택의 실제 행동력 판정 검사
    public IEnumerator SiphonRollsExactlyThirtyPercentOnlyAfterValidHit() // 명중 뒤 행동력 회복의 실제 결과와 난수 경계 검사
    { // 마력 흡수 플레이 모드 검사 시작
        yield return new EnterPlayMode(); // 실제 카드와 전투 연출 모드 시작
        yield return VerifySiphon(); // 잘못된 대상, 성공, 실패 및 빗나감 경계 확인
        yield return new ExitPlayMode(); // 격리 전투 정리 뒤 편집 모드 복귀
    } // 마력 흡수 플레이 모드 검사 종료

    private static IEnumerator VerifySiphon() // 원본 마력 흡수 카드의 정상 입력과 확률 단계
    { // 마력 흡수 결과 확인 시작
        using var fight = new Fight("MANA_SIPHON", "MANA_SIPHON", "MANA_SIPHON", "MANA_SIPHON", "MANA_CONCENTRATION"); // 성공과 실패 및 상한을 검사할 실제 카드 덱
        var owner = fight.Allies[0]; // 명중률과 집중 효과의 실제 소유자
        Assert.IsTrue(owner.ApplyPersistentMental(50)); // 고정 마법 피해의 일반 정신력 조건 설정
        var slot = fight.View("MANA_SIPHON"); // 실제 손패에서 카드의 입력 및 원본 조회
        Assert.AreEqual(1, slot.RuntimeCard.ApCost); // 원본 카드 비용 한 점 확인
        Assert.AreEqual(4, slot.RuntimeCard.EffectValue); // 원본 카드 마법 피해 네 점 확인
        Assert.AreEqual(BattleDamageType.Magical, slot.RuntimeCard.DamageType); // 실제 마법 피해 유형 확인
        fight.Enemies[3].TakeDamage(1000, BattleDamageType.None); // 전투불능 대상 입력을 검사할 실제 적 처치
        Assert.IsTrue(fight.Enemies[3].IsDead); // 비용이나 난수를 쓰지 않을 실제 전투불능 대상 확인
        int seed = FindSeed(29); // 30% 영역의 마지막 허용 난수 시드
        Random.InitState(seed); // 잘못된 선택 직전의 기존 난수 상태 준비
        Random.State beforeInvalid = Random.state; // 잘못된 입력과 취소에 보존될 난수 상태
        slot.OnPointerClick(null); // 현재 생존 적 한 명의 입력을 기다리는 카드 선택
        fight.EnemyViews[3].OnPointerClick(null); // 실제 전투불능 위치를 눌러 잘못된 대상 선택
        Assert.AreSame(slot.RuntimeCard, fight.Controller.SelectedCard); // 잘못 눌러도 원래 대상 선택이 유지됨 확인
        Assert.AreEqual(20, fight.Points.CurrentActionPoints); // 잘못된 대상으로 비용을 내지 않음 확인
        Assert.AreEqual(beforeInvalid, Random.state); // 잘못된 대상 입력이 난수를 소비하지 않음 확인
        slot.OnPointerClick(null); // 선택만 됐던 실제 카드의 정상 취소
        fight.Controller.CardUsed += (card, _) => // 실제 충돌 직전 정확한 경계 난수 고정
        { // 흡수 카드의 실제 확률 시드 연결 시작
            if (card.SourceData.CardId == "CRD_SHEET_MANA_SIPHON") // 현재 정산이 마력 흡수인지 확인
            { // 흡수 난수 설정 시작
                Random.InitState(seed); // 이번 명중과 흡수 경계를 제어할 원래 전투 난수 적용
            } // 흡수 난수 설정 종료
        }; // 흡수 카드의 실제 확률 시드 연결 종료
        var target = fight.Enemies[0]; // 마력 흡수를 반복해서 맞힐 생존 적
        yield return UseSiphon(fight, 0); // 난수 29의 성공하는 실제 마법 공격
        Assert.AreEqual(196, target.CurrentHealth); // 마력 흡수의 첫 네 점 실제 피해 확인
        Assert.AreEqual(20, fight.Points.CurrentActionPoints); // 지불한 한 점을 성공 후 최대치 안에서 돌려받음 확인
        seed = FindSeed(30); // 30% 밖의 첫 실패 난수 시드로 변경
        yield return UseSiphon(fight, 0); // 난수 30의 실패하는 실제 명중 공격
        Assert.AreEqual(192, target.CurrentHealth); // 행동력 회복이 실패해도 정상 마법 피해 적용 확인
        Assert.AreEqual(19, fight.Points.CurrentActionPoints); // 실패한 한 점 회복의 미지급 확인
        yield return fight.Use("MANA_CONCENTRATION"); // 피해 사용이 빗나가도 없어질 실제 집중 상태 획득
        Assert.IsNotNull(owner.GetStatusEffect(BattleStatusEffectType.ManaConcentration)); // 빗나감 전에 집중 상태 확인
        owner.ApplyStatusEffect(BattleStatusEffectType.Blind, 100, 1, 1); // 다음 마력 흡수를 확실히 빗나가게 하는 기존 실명 상태
        seed = FindSeed(29); // 명중할 때 회복이 나올 첫 값으로 빗나감 난수 준비
        Random.InitState(seed); // 원래 명중 판정이 사용할 입력 상태 설정
        Random.Range(0, 100); // 실명 명중 계산의 실제 한 번 난수 소비 재현
        Random.State afterAccuracy = Random.state; // 격리된 명중 판정만 진행한 비교 상태
        yield return UseSiphon(fight, 0); // 영 명중률에서도 정상 지불과 카드 소모 진행
        Assert.AreEqual(192, target.CurrentHealth); // 빗나간 피해가 체력을 변경하지 않음 확인
        Assert.AreEqual(18, fight.Points.CurrentActionPoints); // 빗나간 카드의 지불과 회복 없음 확인
        Assert.IsNull(owner.GetStatusEffect(BattleStatusEffectType.ManaConcentration)); // 모든 타격이 빗나가도 확정 사용으로 집중 소모 확인
        Assert.AreEqual(afterAccuracy, Random.state); // 명중 실패 뒤 추가 행동력 확률 난수를 소비하지 않음 확인
        owner.RemoveStatusEffect(BattleStatusEffectType.Blind); // 정상적으로 명중할 마지막 마력 흡수 준비
        target.DamageResolved += (_, _, _) => fight.Points.Restore(); // 같은 타격에 다른 효과가 행동력을 먼저 채운 상황 재현
        yield return UseSiphon(fight, 0); // 기존 최대치에 도달한 뒤 성공하는 실제 타격
        Assert.AreEqual(188, target.CurrentHealth); // 이미 최대 행동력이어도 일반 타격 경로 유지 확인
        Assert.AreEqual(20, fight.Points.CurrentActionPoints); // 회복으로 현재 행동력이 최대치를 넘지 않음 확인
        Assert.AreEqual(20, fight.Points.MaxActionPoints); // 흡수 회복이 기존 상한을 늘리지 않음 확인
    } // 마력 흡수 결과 확인 종료

    private static IEnumerator UseSiphon(Fight fight, int enemyIndex) // 회복 성공 여부와 별개인 원본 사용 절차
    { // 마력 흡수 클릭 및 연출 확인 시작
        var view = fight.View("MANA_SIPHON"); // 현재 손패의 실제 마력 흡수 화면 선택
        var card = view.RuntimeCard; // 카드 이동을 관찰할 정확한 손패 인스턴스
        view.OnPointerClick(null); // 손패 카드 선택 입력
        fight.EnemyViews[enemyIndex].OnPointerClick(null); // 실제 적 화면을 통해 지정 대상을 확정
        float deadline = Time.realtimeSinceStartup + 10f; // 정상 카드 효과와 복귀까지 기다릴 시간 상한
        while (fight.Runner.IsBusy && Time.realtimeSinceStartup < deadline) // 실제 연출이 추적 가능한 동안 프레임 진행
        { // 카드 연출 프레임 대기 시작
            yield return null; // 다음 실제 전투 프레임 대기
        } // 카드 연출 프레임 대기 종료
        Assert.IsFalse(fight.Runner.IsBusy); // 카드의 충돌과 복귀가 완료됐는지 확인
        CollectionAssert.Contains(fight.Deck.DiscardPile, card); // 사용한 카드의 실제 버린 영역 이동 확인
    } // 마력 흡수 클릭 및 연출 확인 종료

    private static int FindSeed(int desired) // 정확한 첫 정수 난수를 만드는 전투 시드 조회
    { // 경계 시드 검색 시작
        for (int seed = 0; seed < 10000; seed++) // 찾을 값의 실제 순서 재현 후보 확인
        { // 개별 시드 검사 시작
            Random.InitState(seed); // 후보 시드의 전투 난수 시작 위치 설정
            if (Random.Range(0, 100) == desired) // 선택한 범위 경계의 첫 값 비교
            { // 원하는 시드 검색 성공 시작
                return seed; // 실제 경계값이 나오는 재현 시드 반환
            } // 원하는 시드 검색 성공 종료
        } // 개별 시드 검사 종료
        throw new AssertionException("요청한 확률 경계의 전투 시드 없음"); // 전투 시드 탐색 실패 결과
    } // 경계 시드 검색 종료
} // 마력 카드 회귀 검사 종료
