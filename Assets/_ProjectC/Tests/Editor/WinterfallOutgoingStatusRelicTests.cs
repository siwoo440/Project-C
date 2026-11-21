using System.Collections; // 실제 카드 플레이 모드 검사 사용
using System.Linq; // 실제 적용 유물의 원본 검색 사용
using NUnit.Framework; // 고정 피해와 발생 여부 검사 사용
using UnityEngine; // 게임 원본과 실제 정수 난수 사용
using UnityEngine.TestTools; // 테스트의 플레이 모드 전환 사용
using Fight = WinterfallSwordCombatTests.Fight; // 기존 카드 클릭과 전투 구성 사용

public sealed class WinterfallOutgoingStatusRelicTests // 침식과 표식 및 확률 공격 유물 검사
{ // 적 적용 유물 검사 시작
    [UnityTest] // 분류 명시 상태와 실제 카드 표식의 적용 검사 지정
    public IEnumerator ActualCorrosionAndCardMarksTriggerOnlyTheirOwnBonuses() // 성공한 침식 상태와 유효한 카드 표식의 별개 처리 확인
    { // 상태 발동 플레이 모드 시작
        yield return new EnterPlayMode(); // 실제 전투 카드 입력 환경 진입
        yield return CheckStatuses(); // 플레이 도메인에서 카드 상태 효과 확인
        yield return new ExitPlayMode(); // 실제 전투 환경 종료
    } // 상태 발동 플레이 모드 종료

    [UnityTest] // 실제 명시 실명과 활성 약화 치명타 경계 검사 지정
    public IEnumerator BlindLensAndDebuffedCriticalUseActualAuthoredAndTimedHitState() // 실제 실명 확률과 현재 약화의 치명타 적용 확인
    { // 실명과 치명타 플레이 모드 시작
        yield return new EnterPlayMode(); // 실제 전투 난수와 카드 입력 환경 진입
        yield return CheckProbabilities(); // 플레이 도메인에서 경계 확률 확인
        yield return new ExitPlayMode(); // 실제 카드 입력 환경 종료
    } // 실명과 치명타 플레이 모드 종료

    private static IEnumerator CheckStatuses() // 침식 분류의 성공 피해와 표식 방어 약화의 실제 수명 확인
    { // 상태 발동 검사 시작
        RelicData core = Hold(RelicEffectType.CorrosionDebuffDamage); // 등록된 침식 맥동핵 실제 보유
        RelicData eye = Hold(RelicEffectType.MarkPhysicalDefenseLoss); // 등록된 결정눈 실제 보유
        Assert.AreEqual(3, core.EffectValue); // 성공한 침식 상태 한 번의 원본 피해량 확인
        Assert.AreEqual(1, eye.EffectValue); // 성공한 표식 카드의 원본 방어 감소량 확인
        Assert.AreEqual(2, eye.StatusDuration); // 실제 적 턴 두 번의 유지 기간 확인
        CardData corrosion = CreateCard("TEST_CORROSION", CardEffectType.ApplyStatusEffect, BattleStatusEffectType.Poison, corrosion: true); // 본 테스트 원본만 명시적으로 침식 계열인 약화
        CardData mark = CreateCard("TEST_MARK", CardEffectType.ApplyStatusEffect, BattleStatusEffectType.PublicMark); // 부여가 명시된 적 공명 표식 검사 원본
        try // 시험 종료 후 임시 카드 원본 정리 보장
        { // 실제 유물 상태 전투 검사 시작
            using (var fight = new Fight(new[] { corrosion, corrosion, corrosion, mark, mark })) // 실제 두 유물 적용과 면역 비교 카드 구성
            { // 발동 경계 확인 시작
                BattleUnitRuntime immune = fight.Enemies[0]; // 침식과 표식을 모두 차단할 적
                BattleUnitRuntime debuffed = fight.Enemies[1]; // 실제 침식 피해와 중첩을 받을 적
                BattleUnitRuntime initialMark = fight.Enemies[2]; // 전투 카드 선택 없이 시작 표식을 받는 적
                BattleUnitRuntime marked = fight.Enemies[3]; // 일반 방어 약화와 표식 약화를 함께 받을 적
                immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 6, 1); // 실제 디버프 면역 활성화
                initialMark.ApplyStatusEffect(BattleStatusEffectType.PublicMark, 1, 1, 1); // 원본 카드가 없는 시작 유물 표식과 같은 적용
                Assert.IsNull(initialMark.GetStatusEffect(BattleStatusEffectType.CrystalMarkDefenseDown)); // 카드에서 발생하지 않은 시작 표식 제외 확인
                debuffed.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseDown, 1, 5, 1, fight.Allies[0]); // 분류하지 않은 아군 원인의 일반 약화 적용
                Assert.AreEqual(200, debuffed.CurrentHealth); // 명시 침식 분류 없는 약화의 임의 가산 방지
                yield return Play(fight, corrosion, 0); // 실제 카드에서 면역 적 침식 시도
                Assert.AreEqual(200, immune.CurrentHealth); // 막힌 침식에 실제 체력 피해 없음 확인
                Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.Poison)); // 맥동핵 검사 상태의 면역 적용 확인
                debuffed.GainBlock(2); // 침식 부가 피해를 실제로 일부 흡수할 방어도 준비
                yield return Play(fight, corrosion, 1); // 일반 적에 실제 침식 계열 상태 첫 적용
                Assert.AreEqual(199, debuffed.CurrentHealth); // 일반 피해 삼 중 기존 방어도 이 흡수 확인
                yield return Play(fight, corrosion, 1); // 실제 침식 상태의 성공한 두 번째 중첩 카드 사용
                Assert.AreEqual(196, debuffed.CurrentHealth); // 성공한 중첩에도 별도 직접 카드가 아닌 피해 삼 확인
                Assert.AreEqual(2, debuffed.GetStatusEffect(BattleStatusEffectType.Poison)?.StackCount); // 실제 두 침식 상태의 중첩 확인
                yield return Play(fight, mark, 0); // 상태에 면역인 적에게 실제 카드 표식 요청
                Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.CrystalMarkDefenseDown)); // 실패한 표식의 방어 감소 차단 확인
                marked.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseUp, 6, 8, 1); // 가산 약화를 정수로 검증할 원래 방어 증가 준비
                marked.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseDown, 2, 8, 1); // 별개로 유지할 더 높은 원래 방어 감소 준비
                Assert.AreEqual(4, marked.PhysicalDefense); // 결정눈 발동 전의 실제 물리 방어 확인
                yield return Play(fight, mark, 3); // 실제 카드에서 유효한 공명 표식 성공
                Assert.AreEqual(3, marked.PhysicalDefense); // 기존 약화의 수치와 별개로 결정눈 방어 일 감소 확인
                Assert.AreEqual(2, marked.GetStatusEffect(BattleStatusEffectType.CrystalMarkDefenseDown)?.RemainingTurns); // 결정눈의 새 적 턴 두 번 확인
                Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 첫 적 턴으로 진행
                Assert.IsNull(marked.GetStatusEffect(BattleStatusEffectType.PublicMark)); // 공명 표식의 고유한 아군 턴 종료 만료 확인
                Assert.AreEqual(3, marked.PhysicalDefense); // 원래 표식 해제 이후에도 별개 적 턴 약화 유지 확인
                Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 실제 첫 적 턴 종료 처리
                Assert.AreEqual(1, marked.GetStatusEffect(BattleStatusEffectType.CrystalMarkDefenseDown)?.RemainingTurns); // 첫 적 턴을 한 번만 계산하는지 확인
                Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 두 번째 실제 적 턴 시작
                Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 두 번째 실제 적 턴 종료
                Assert.IsNull(marked.GetStatusEffect(BattleStatusEffectType.CrystalMarkDefenseDown)); // 두 적 턴 종료에만 실제 방어 약화 해제 확인
                Assert.AreEqual(4, marked.PhysicalDefense); // 다른 원래 방어 감소의 수치와 수명 보존 확인
            } // 발동 경계 확인 종료
            using (var fight = new Fight("BLOODSTAINED_DUEL")) // 실제 상점의 양쪽 결투 표식 카드 원본 준비
            { // 결투 표식의 진영 확인 시작
                yield return fight.Use("BLOODSTAINED_DUEL", 0); // 실제 결투 카드를 적에게 적용
                Assert.IsNotNull(fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.CrystalMarkDefenseDown)); // 결투가 성립한 적에게만 결정눈 약화 확인
                Assert.IsNull(fight.Allies[0].GetStatusEffect(BattleStatusEffectType.CrystalMarkDefenseDown)); // 결투의 아군 쪽 표식에서 유물 발동 차단 확인
            } // 결투 표식의 진영 확인 종료
        } // 실제 유물 상태 전투 검사 종료
        finally // 임시 카드 원본 정리 시작
        { // 테스트 카드 정리 시작
            Object.Destroy(corrosion); // 시험으로 명시한 침식 카드 자료 제거
            Object.Destroy(mark); // 시험으로 명시한 공명 카드 자료 제거
        } // 테스트 카드 정리 종료
    } // 상태 발동 검사 종료

    private static IEnumerator CheckProbabilities() // 실제 확률 경계와 만료 상태 치명타 비교
    { // 실명과 치명타 검사 시작
        Assert.AreEqual(10, Hold(RelicEffectType.DebuffedTargetCriticalChance).EffectValue); // 균열침의 실제 확률 가산량 확인
        Assert.AreEqual(15, Hold(RelicEffectType.BlindApplicationChanceBonus).EffectValue); // 공허 렌즈의 실제 확률 가산량 확인
        CardData optional = CreateCard("TEST_BLIND_ZERO", CardEffectType.ApplyStatusEffect, BattleStatusEffectType.Blind, chance: 0); // 명시 확률 영의 실제 실명 원본 준비
        CardData plain = CreateCard("TEST_BLIND_PLAIN", CardEffectType.ApplyStatusEffect, BattleStatusEffectType.Blind); // 기존처럼 확률 필드가 없는 보장 실명 원본 준비
        CardData onHit = CreateCard("TEST_BLIND_HIT", CardEffectType.Damage, BattleStatusEffectType.Blind, chance: 100); // 실제 명중 후 보장 실명 원본 준비
        CardData off = CreateCard("TEST_BLIND_OFF", CardEffectType.Damage, BattleStatusEffectType.Blind); // 명시 확률이 없으므로 명중 시 실명 없음 원본 준비
        CardData hit = CreateCard("TEST_HIT", CardEffectType.Damage, BattleStatusEffectType.None); // 다른 치명타 확률을 갖지 않은 기본 타격 원본
        try // 임시 자료 정리 보장
        { // 실제 공격 난수 검사 시작
            using var fight = new Fight(new[] { optional, optional, plain, onHit, off, hit, hit, hit }); // 각 확률 경계의 실제 카드를 모두 가진 독립 전투
            int seed = 0; // 실제 카드 충돌 직전 적용할 현 난수 시드
            fight.Controller.CardUsed += (_, _) => Random.InitState(seed); // 전투 연출과 분리한 실제 확률 선택 시점 조절
            seed = SeedFor(15); // 렌즈를 더한 십오 퍼센트에서 실패하는 경계
            yield return Play(fight, optional, 0); // 명시 확률 영이 십오로 올라가도 실패하는 실제 카드 사용
            Assert.IsNull(fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.Blind)); // 정수 십오의 실패 확인
            seed = SeedFor(14); // 십오 퍼센트에서 성공하는 가장 높은 난수
            yield return Play(fight, optional, 1); // 실제 렌즈에게 성공한 명시 영 퍼센트 실명 사용
            Assert.IsNotNull(fight.Enemies[1].GetStatusEffect(BattleStatusEffectType.Blind)); // 정수 십사의 실제 성공 확인
            yield return Play(fight, plain, 0); // 새 확률 필드가 없는 기존 직접 실명의 정상 사용
            Assert.IsNotNull(fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.Blind)); // 명시 확률이 없던 확정 실명 보존 확인
            Assert.AreEqual(14, Random.Range(0, 100)); // 준비한 확률을 기존 보장 실명이 소비하지 않았는지 확인
            yield return Play(fight, onHit, 2); // 보장 실명을 실제 직접 명중 뒤 적용
            Assert.IsNotNull(fight.Enemies[2].GetStatusEffect(BattleStatusEffectType.Blind)); // 실제 공격 명중 뒤 독립 실명 확인
            Assert.AreEqual(14, Random.Range(0, 100)); // 백 퍼센트 실명과 원래 확정 명중의 무작위 미소비 확인
            yield return Play(fight, off, 3); // 명시 확률이 없는 별개 공격의 실명 비활성 경우 확인
            Assert.IsNull(fight.Enemies[3].GetStatusEffect(BattleStatusEffectType.Blind)); // 실명 상태 원본만으로 미설정 공격을 활성화하지 않는지 확인
            Assert.AreEqual(14, Random.Range(0, 100)); // 미설정 공격 실명의 난수 미소비 확인
            seed = SeedFor(9); // 실제 약화의 십 퍼센트 치명타 마지막 성공 경계
            yield return Play(fight, hit, 1); // 실제 활성 실명으로 약화 상태인 적에게 일반 명중
            Assert.AreEqual(185, fight.Enemies[1].CurrentHealth); // 일반 피해 십의 실제 치명타 십오 확인
            seed = SeedFor(10); // 같은 십 퍼센트 치명타 첫 실패 경계
            yield return Play(fight, hit, 1); // 동일 약화 적에게 실제 치명타 실패 카드 사용
            Assert.AreEqual(175, fight.Enemies[1].CurrentHealth); // 치명타 실패 시 기본 피해 십 확인
            var expired = fight.Enemies[1].GetStatusEffect(BattleStatusEffectType.Blind); // 목록에 남지만 만료한 상태를 확인할 실명 조회
            while (!expired.IsExpired) // 일반 처리 제거 없이 활성 기간만 종료
            { // 만료 경계 설정 시작
                expired.AdvanceDuration(); // 기존 상태의 남은 기간 한 번 감소
            } // 만료 경계 설정 종료
            seed = SeedFor(9); // 활성 약화라면 치명타가 날 동일 성공 시드
            yield return Play(fight, hit, 1); // 명중 순간에 더 이상 활성 약화가 없는 경우
            Assert.AreEqual(165, fight.Enemies[1].CurrentHealth); // 만료한 실명으로 치명타 확률을 올리지 않는지 확인
            Assert.AreEqual(9, Random.Range(0, 100)); // 활성 약화 없는 공격의 치명타 난수 미소비 확인
        } // 실제 공격 난수 검사 종료
        finally // 임시 자료 정리 시작
        { // 검사 원본 제거 시작
            foreach (CardData card in new[] { optional, plain, onHit, off, hit }) // 실제 테스트 카드 원본 순회
            { // 카드별 자료 해제 시작
                Object.Destroy(card); // 독립 테스트 카드 원본 제거
            } // 카드별 자료 해제 종료
        } // 검사 원본 제거 종료
    } // 실명과 치명타 검사 종료

    private static CardData CreateCard(string id, CardEffectType type, BattleStatusEffectType status, int chance = -1, bool corrosion = false) // 별도의 확률과 계열을 명시하는 실제 검사 원본
    { // 카드 자료 생성 시작
        var card = ScriptableObject.CreateInstance<CardData>(); // 검사 카드 원본 생성
        string optional = chance >= 0 ? $",\"optionalBlindChancePercent\":{chance}" : ""; // 미설정 자료에서 새 필드 자체를 생략
        JsonUtility.FromJsonOverwrite($"{{\"cardId\":\"{id}\",\"displayName\":\"{id}\",\"targetType\":{(int)CardTargetType.SingleEnemy},\"effectType\":{(int)type},\"statusEffectType\":{(int)status},\"effectValue\":10,\"secondaryEffectValue\":20,\"statusDuration\":2,\"statusMaximumStacks\":2,\"isCorrosionStatus\":{(corrosion ? "true" : "false")}{optional}}}", card); // 실제 카드의 원본 효과와 명시 계열 및 설정 확률 입력
        return card; // 실제 카드 컨트롤러로 사용할 원본 반환
    } // 카드 자료 생성 종료

    private static IEnumerator Play(Fight fight, CardData card, int enemy) // 실제 손패와 적 클릭으로 카드 사용
    { // 기존 플레이 검증 연결 시작
        return WinterfallCommonCardExpansionTests.Play(card, fight.Hand, fight.EnemyViews[enemy], fight.Runner, fight.Deck, fight.Points); // 실제 카드 이동과 행동력 및 연출 검사 사용
    } // 기존 플레이 검증 연결 종료

    private static RelicData Hold(RelicEffectType effect) // 실제 등록한 유물을 회차에 획득
    { // 보유 유물 준비 시작
        RelicData relic = Resources.LoadAll<RelicData>("Relics").Single(item => item.EffectType == effect); // 효과와 일치하는 새 게임 유물 원본 조회
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 같은 게임 경로로 정상 보유 확인
        return relic; // 정확한 데이터 비교에 사용할 원본 반환
    } // 보유 유물 준비 종료

    private static int SeedFor(int roll) // 실제 유니티 정수 난수의 경계 시드 조회
    { // 확률 시드 탐색 시작
        Random.State previous = Random.state; // 이전 난수 흐름 저장
        try // 시드 탐색의 전역 난수 복원 보장
        { // 실제 경계 탐색 시작
            for (int seed = 0; seed < 10000; seed++) // 정확한 난수 경계의 후보 시드 순회
            { // 난수 후보 비교 시작
                Random.InitState(seed); // 해당 후보의 난수 초기화
                if (Random.Range(0, 100) == roll) // 지정 실제 난수 결과와 비교
                { // 정확한 경계 확인 시작
                    return seed; // 실제 난수 경계를 만든 시드 반환
                } // 정확한 경계 확인 종료
            } // 난수 후보 비교 종료
            throw new System.InvalidOperationException($"난수 {roll}의 검사 시드 없음"); // 실제 게임 난수에서 경계 후보를 찾지 못한 경우 검사 실패
        } // 실제 경계 탐색 종료
        finally // 방금 이전의 난수 흐름 복구
        { // 기존 난수 준비 시작
            Random.state = previous; // 테스트 간 전역 난수 상태 복구
        } // 기존 난수 준비 종료
    } // 확률 시드 탐색 종료
} // 적 적용 유물 검사 종료
