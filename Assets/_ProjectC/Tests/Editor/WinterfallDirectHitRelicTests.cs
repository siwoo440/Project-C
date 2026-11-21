using System.Collections; // 실제 카드 연출 대기
using NUnit.Framework; // 명중 피해 결과 비교
using UnityEngine; // 전투 카드와 유물 원본 조회
using UnityEngine.TestTools; // 플레이 모드 카드 입력 검사
using Fight = WinterfallSwordCombatTests.Fight; // 기존 실제 전투 검사 환경 사용

public sealed class WinterfallDirectHitRelicTests // 직접 명중 유물 세 종류의 실제 입력 검사
{ // 직접 명중 유물 검사 시작
    [UnityTest] // 수정못과 안경 렌즈의 연타 가산 검사
    public IEnumerator WandAndBlindDamageStackWithExistingMarkOnEveryHit() // 실제 지팡이와 실명 및 표식의 타격별 피해 비교
    { // 수정못과 렌즈 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 진입 완료 대기
        yield return VerifyWandAndBlind(); // 실제 카드 사용과 타격 비교
        yield return new ExitPlayMode(); // 플레이 모드 종료 완료 대기
    } // 수정못과 렌즈 검사 종료

    private static IEnumerator VerifyWandAndBlind() // 피해 속성과 카드 종류가 다른 연타 입력
    { // 연타 입력 검사 시작
        Acquire("SmallCrystalNail"); // 작은 수정못 원본 획득
        Acquire("BrokenSpectacleLens"); // 깨진 안경 렌즈 원본 획득
        Acquire("MarkingChalk"); // 기존 공명 표식 피해 가산 유물 획득
        var wand = Attack("TEST_WAND_DOUBLE", CardType.Wand, BattleDamageType.Physical, 2); // 물리 피해 지팡이 연타 원본 생성
        var sword = Attack("TEST_MAGIC_DOUBLE", CardType.Sword, BattleDamageType.Magical, 2); // 마법 피해 비지팡이 연타 원본 생성
        try // 임시 카드 원본 정리 보장
        { // 연타용 실제 전투 시작
            using var fight = new Fight(new[] { wand, sword, wand }); // 세 번 공격할 실제 덱과 전투 구성
            fight.Enemies[0].ApplyStatusEffect(BattleStatusEffectType.Blind, 20, 1, 1); // 첫 적에게 실제 실명 부여
            fight.Enemies[0].ApplyStatusEffect(BattleStatusEffectType.PublicMark, 1, 1, 1); // 첫 적에게 기존 공명 표식 부여
            fight.Enemies[1].ApplyStatusEffect(BattleStatusEffectType.Blind, 20, 1, 1); // 둘째 적에게 실명만 부여
            yield return UseAt(fight, wand, 0, 26); // 각 명중의 기본 칠과 수정못 일과 렌즈 삼과 분필 이 합산
            yield return UseAt(fight, sword, 1, 20); // 비지팡이 마법 연타의 각 명중에 렌즈만 가산
            yield return UseAt(fight, wand, 2, 16); // 실명 없는 적의 각 명중에 수정못만 가산
        } // 연타용 실제 전투 종료
        finally // 임시 카드 원본 정리 시작
        { // 정리 대상 순서 시작
            Object.Destroy(wand); // 임시 지팡이 원본 정리
            Object.Destroy(sword); // 임시 마법 검 원본 정리
        } // 정리 대상 순서 종료
    } // 연타 입력 검사 종료

    [UnityTest] // 전투 수첩의 연속 명중과 예약 소모 검사
    public IEnumerator NotebookPrimesAfterThreeLandedHitsAndSpendsOnTheNextLandedHit() // 빗나감과 전체 방어 흡수 및 대상 변경 경계 확인
    { // 전투 수첩 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 진입 완료 대기
        yield return VerifyNotebook(); // 실제 손패와 대상 입력으로 연속 명중 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료 완료 대기
    } // 전투 수첩 검사 종료

    private static IEnumerator VerifyNotebook() // 전투 수첩의 연속 기록과 보너스 초기화 확인
    { // 연속 기록 검사 시작
        Acquire("OldBattleNotebook"); // 오래된 전투 수첩 원본 획득
        var single = Attack("TEST_SINGLE", CardType.Sword, BattleDamageType.Physical, 1); // 기본 피해 칠의 한 번 공격 생성
        var twice = Attack("TEST_DOUBLE", CardType.Sword, BattleDamageType.Physical, 2); // 같은 대상을 두 번 공격할 원본 생성
        try // 임시 카드 원본 정리 보장
        { // 연속 기록용 실제 전투 시작
            using var fight = new Fight(new[] { twice, single, twice, single, single, single, single, twice, single, single }); // 연속 명중과 경계를 모두 확인할 손패 구성
            var first = fight.Enemies[0]; // 연속 기록을 시작할 첫 적
            var second = fight.Enemies[1]; // 연속 기록 변경 및 보너스 대상 적
            yield return UseAt(fight, twice, 0, 14); // 첫 적에게 두 번 명중 기록
            yield return UseAt(fight, single, 1, 7); // 둘째 적 실제 명중으로 연속 기록 변경
            yield return UseAt(fight, twice, 0, 14); // 첫 적에게 다시 명중 두 번부터 기록
            first.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 1, 1); // 첫 적의 확정 회피 설정
            yield return UseAt(fight, single, 0, 0); // 빗나감으로 연속 명중을 채우지 않음 확인
            first.RemoveStatusEffect(BattleStatusEffectType.Evasion); // 다음 실제 명중용 회피 해제
            first.ApplyStatusEffect(BattleStatusEffectType.Bleed, 2, 1, 1); // 비공격 피해 확인용 출혈 부여
            int beforeBleed = first.CurrentHealth; // 지속 피해 전 첫 적 체력 기록
            new BattleStatusEffectProcessor().ProcessPhaseEnd(first, fight.Turns.CurrentRound); // 기존 상태 처리로 출혈 실제 발동
            Assert.AreEqual(2, beforeBleed - first.CurrentHealth); // 직접 명중이 아닌 실제 체력 피해 확인
            Assert.IsTrue(fight.Allies[0].TrySpendHealth(1)); // 직접 명중이 아닌 아군 체력 비용 지불
            yield return UseAt(fight, single, 0, 7); // 비공격 피해를 건너뛴 세 번째 실제 명중으로 예약
            second.ApplyStatusEffect(BattleStatusEffectType.Evasion, 100, 1, 1); // 예약 상태에서 다른 적의 확정 회피 설정
            yield return UseAt(fight, single, 1, 0); // 빗나감으로 예약 보너스를 소모하지 않음 확인
            second.RemoveStatusEffect(BattleStatusEffectType.Evasion); // 실제 보너스 명중용 회피 해제
            yield return UseAt(fight, single, 1, 11); // 다른 적에게도 바로 다음 명중의 추가 피해 사 지급
            yield return UseAt(fight, twice, 1, 14); // 보너스 타격을 제외하고 기록 두 번 재시작
            second.GainBlock(7); // 다음 한 번의 피해를 전부 흡수할 방어도 지급
            yield return UseAt(fight, single, 1, 0); // 실제 명중하되 체력 피해가 없는 세 번째 기록
            Assert.Zero(second.CurrentBlock); // 빗나감과 구분되는 방어도 흡수 확인
            yield return UseAt(fight, single, 2, 11); // 세 번째 적에 재예약된 보너스의 한 번 적용 확인
        } // 연속 기록용 실제 전투 종료
        finally // 임시 카드 원본 정리 시작
        { // 정리 대상 순서 시작
            Object.Destroy(single); // 임시 단타 원본 정리
            Object.Destroy(twice); // 임시 연타 원본 정리
        } // 정리 대상 순서 종료
    } // 연속 기록 검사 종료

    private static void Acquire(string name) // 실제 회차 유물 원본 획득 검사
    { // 유물 획득 시작
        var relic = Resources.Load<RelicData>("Relics/" + name); // 실제 보상 목록과 연결된 유물 조회
        Assert.IsNotNull(relic, name); // 등록된 유물 원본 확인
        Assert.AreEqual(RelicAcquireResult.Acquired, RelicRunManager.EnsureInstance().TryAcquire(relic)); // 실제 유물 보관함에 지급 확인
    } // 유물 획득 종료

    private static CardData Attack(string id, CardType type, BattleDamageType damageType, int hitCount) // 카드 속성과 피해 종류를 분리한 검사 원본 생성
    { // 검사 카드 생성 시작
        var card = ScriptableObject.CreateInstance<CardData>(); // 실제 카드 규칙을 쓰는 임시 원본 생성
        JsonUtility.FromJsonOverwrite($"{{\"cardId\":\"{id}\",\"displayName\":\"{id}\",\"cardType\":{(int)type},\"targetType\":{(int)CardTargetType.SingleEnemy},\"effectType\":{(int)CardEffectType.Damage},\"damageType\":{(int)damageType},\"effectValue\":7,\"damageHitCount\":{hitCount}}}", card); // 기본 피해와 대상 및 개별 타격 수 설정
        return card; // 완성한 검사 카드 반환
    } // 검사 카드 생성 종료

    private static IEnumerator UseAt(Fight fight, CardData card, int targetIndex, int expectedHealthDamage) // 기존 카드 선택과 대상 클릭으로 피해 비교
    { // 실제 공격 수행 시작
        var enemy = fight.Enemies[targetIndex]; // 실제로 선택할 적의 전투 객체 조회
        int before = enemy.CurrentHealth; // 카드 사용 전 적 체력 기록
        yield return WinterfallCommonCardExpansionTests.Play(card, fight.Hand, fight.EnemyViews[targetIndex], fight.Runner, fight.Deck, fight.Points); // 손패와 대상 화면을 거친 실제 카드 사용
        Assert.AreEqual(expectedHealthDamage, before - enemy.CurrentHealth, card.CardId); // 한 카드가 적용한 전체 체력 피해 검증
    } // 실제 공격 수행 종료
} // 직접 명중 유물 검사 종료
