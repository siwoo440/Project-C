using System; // 임시 유물과 전투 원본 정리
using System.Collections; // 플레이 모드 및 카드 연출 대기
using NUnit.Framework; // 실제 피해와 확률 상태의 비교
using UnityEngine; // 전투 원본과 실제 임의 정수 조회
using UnityEngine.TestTools; // 실제 Unity 플레이 모드 검사
using Fight = WinterfallSwordCombatTests.Fight; // 원래 손패 카드와 적 입력 시험
using Object = UnityEngine.Object; // 검사 원본 제거
using Random = UnityEngine.Random; // 실제 상태 판정 재현

public sealed class WinterfallPincerAndConveyorTests // 행동력 감소와 기계형 출혈 변환의 실제 전투 회귀
{ // 전투 유물 검사 시작
    [UnityTest] // 상태 종류, 기간, 예고와 직접 카드 피해의 원래 결과
    public IEnumerator PincerUsesActiveActionPointDownWithoutTreatingStaggerAsEnergyLoss() // 유효 행동력 감소의 실제 직접 피해와 비교 상태 검증
    { // 타격 가산 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 화면과 유물 회차 준비
        using (var owned = new OwnedRelic("CompressionPincerHandle")) // 압착 집게 손잡이 원본 실제 장착
        using (var fight = new Fight("RUNE_BURST", "RUNE_BURST")) // 정상 단일 마법 카드 두 장 준비
        { // 상태와 예고를 실제 카드 타격에 연결 시작
            var ally = fight.Allies[0]; // 두 비교 타격의 실제 원본 아군
            var affected = fight.Enemies[0]; // 실제 행동력 감소와 방어도를 받을 적
            var staggered = fight.Enemies[1]; // 기존 행동 취소만 가진 비교 적
            var expired = fight.Enemies[2]; // 유효 기간이 끝난 상태만 남은 적
            Assert.AreEqual(BattleStatusEffectApplyResult.Applied, affected.ApplyStatusEffect(BattleStatusEffectType.ActionPointDown, 1, 1, 1, ally)); // 실제 부정 상태의 원래 적용 경로
            Assert.IsTrue(affected.GetStatusEffect(BattleStatusEffectType.ActionPointDown).IsDebuff); // 약화 면역과 정화가 인식할 부정 상태
            staggered.ApplyStatusEffect(BattleStatusEffectType.Stagger, 1, 1, 1, ally); // 전투 상태가 다르지만 비슷한 기존 행동 취소
            expired.ApplyStatusEffect(BattleStatusEffectType.ActionPointDown, 1, 1, 1, ally); // 실제 시간표가 끝난 상태 데이터를 위한 원본
            expired.GetStatusEffect(BattleStatusEffectType.ActionPointDown).AdvanceDuration(); // 제거 전 만료된 독립 해제 경계
            Assert.AreEqual(20, affected.PreviewDamage(19, BattleDamageType.Magical, ally, alliedDirectAttack: true).RawDamage); // 열 퍼센트 내림을 반영한 원래 직접 피해 예고
            Assert.AreEqual(19, staggered.PreviewDamage(19, BattleDamageType.Magical, ally, alliedDirectAttack: true).RawDamage); // 경직은 행동력 감소 분류가 아님 확인
            Assert.AreEqual(19, expired.PreviewDamage(19, BattleDamageType.Magical, ally, alliedDirectAttack: true).RawDamage); // 종료 상태의 예고 가산 없음 확인
            Assert.AreEqual(11, affected.TakeDamage(11, BattleDamageType.None, ally).AppliedDamage); // 직접 카드와 구별되는 일반 피해 보존
            affected.GainBlock(4); // 원래 마법 피해 가산 뒤 차감할 실제 방어도
            BattleDamageResult result = default; // 실제 직접 피해의 이벤트 확인
            affected.DamageTaken += (_, damage) => result = damage; // 원래 카드의 실제 피해 결과 보관
            yield return fight.Use("RUNE_BURST", 0); // 활성 행동력 감소 적에 정상 손패 마법 명중
            Assert.AreEqual(12, result.RawDamage); // 열하나의 원래 피해와 내림한 한 점 가산
            Assert.AreEqual(8, result.AppliedDamage); // 원래 방어도 네 점을 같은 경로에서 차감
            Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 상태 대상의 실제 적 턴 시작과 끝
            Assert.IsNull(affected.GetStatusEffect(BattleStatusEffectType.ActionPointDown)); // 일반 유효 기간과 정화 가능한 상태 수명 확인
            yield return fight.Use("RUNE_BURST", 0); // 상태가 끝난 이후의 같은 실제 원본 타격
            Assert.AreEqual(11, result.RawDamage); // 만료 이후 실제 기본 피해 정상 복원
        } // 상태와 예고를 실제 카드 타격에 연결 종료
        yield return new ExitPlayMode(); // 독립 체크 환경 정리
    } // 타격 가산 검사 종료

    [UnityTest] // 실제 서리 감옥의 단일 행동 취소와 새 직접 피해 조건
    public IEnumerator FrostPrisonMarksOnlyItsSuccessfullyAppliedNativeStaggerUntilTheEnemyAction() // 실제 카드의 고유 상태 출처와 원래 소비 확인
    { // 서리 감옥의 실제 출처 검사 시작
        yield return new EnterPlayMode(); // 정상 카드와 적 예고를 쓰는 플레이 모드 준비
        using (var owned = new OwnedRelic("CompressionPincerHandle")) // 실제 압착 집게 손잡이 보유
        using (var fight = new Fight("FROST_PRISON", "FROST_PRISON", "FROST_PRISON", "RUNE_BURST")) // 원래 확률 서리 감옥과 다음 실제 타격 준비
        { // 성공과 면역 및 난수 실패 대조 시작
            var ally = fight.Allies[0]; // 행동력을 줄인 실제 카드 소유자
            var target = fight.Enemies[0]; // 정상적으로 행동을 잃을 적
            var immune = fight.Enemies[1]; // 기존 약화 면역 대상
            var failed = fight.Enemies[2]; // 성공 범위를 벗어난 적
            immune.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 실제 서리 감옥의 정상 약화 면역
            int seed = FindSeed(39); // 기존 사십 퍼센트 확률의 마지막 성공 난수
            fight.Controller.CardUsed += (_, __) => Random.InitState(seed); // 실제 카드 명중과 상태 판정의 재현 난수
            int staggerSignals = 0; // 기본 경직과 피아노 줄이 받는 원래 상태 신호 집계
            int extraSignals = 0; // 별도 상태를 잘못 부여하는 중복 신호의 대조
            int damageInSignal = 0; // 실제 상태 신호가 확인할 활성 출처
            target.StatusEffectResolved += (_, __, type, outcome) => // 같은 대상의 실제 정상 상태 결과 관찰
            { // 성공한 적용 결과만 새로 집계
                if (type == BattleStatusEffectType.Stagger && outcome == BattleStatusEffectApplyResult.Applied) // 원래 서리 감옥 경직 한 건 확인
                { // 유효한 실제 경직 신호 처리 시작
                    staggerSignals++; // 기존 경직 처리 경로가 받은 한 건
                    damageInSignal = target.PreviewDamage(19, BattleDamageType.Magical, ally, alliedDirectAttack: true).RawDamage; // 같은 상태 신호의 실제 행동력 감소 보정
                } // 유효한 실제 경직 신호 처리 종료
                if (type == BattleStatusEffectType.ActionPointDown) // 별도 상태를 추가하는 중복 여부 확인
                { // 추가 공개 상태 신호 확인 시작
                    extraSignals++; // 서리 감옥에서 생기면 안 되는 별도 상태 결과
                } // 추가 공개 상태 신호 확인 종료
            }; // 정식 상태 적용 결과 관찰 등록 종료
            yield return fight.Use("FROST_PRISON", 0); // 명중하고 실제 경직 확률도 성공한 원래 카드 사용
            Assert.AreEqual(1, staggerSignals); // 일반 경직과 피아노 줄을 위한 공개 상태 신호가 한 번뿐인지 확인
            Assert.AreEqual(0, extraSignals); // 같은 원본 적용에 별도 행동력 감소 상태를 추가하지 않음 확인
            Assert.AreEqual(20, damageInSignal); // 정상 공개 신호 전에 실제 경직 인스턴스에 출처 반영
            Assert.IsNull(target.GetStatusEffect(BattleStatusEffectType.ActionPointDown)); // 서리 감옥이 만든 상태는 기존 경직 그대로 확인
            yield return fight.Use("FROST_PRISON", 1); // 기존 약화 면역으로 취소되는 정상 실제 명중
            Assert.IsNull(immune.GetStatusEffect(BattleStatusEffectType.Stagger)); // 기존 약화 면역과 상태 미적용 확인
            Assert.AreEqual(19, immune.PreviewDamage(19, BattleDamageType.Magical, ally, alliedDirectAttack: true).RawDamage); // 막힌 상태에서 집게 가산 없음 확인
            seed = FindSeed(40); // 기존 사십 퍼센트 확률의 첫 실패 난수
            yield return fight.Use("FROST_PRISON", 2); // 실제 타격은 맞았으나 후속 적용이 실패한 원본
            Assert.IsNull(failed.GetStatusEffect(BattleStatusEffectType.Stagger)); // 실패한 일반 경직 시도의 상태 미생성
            Assert.AreEqual(19, failed.PreviewDamage(19, BattleDamageType.Magical, ally, alliedDirectAttack: true).RawDamage); // 시도 실패의 출처 미등록 확인
            BattleDamageResult landed = default; // 원래 지팡이 타격의 실제 피해 결과
            target.DamageTaken += (_, value) => landed = value; // 정상 카드 명중의 피해 원본 확인
            yield return fight.Use("RUNE_BURST", 0); // 서리 감옥으로 행동을 잃은 대상으로 정상 추가 타격
            Assert.AreEqual(12, landed.RawDamage); // 서리 감옥 상태가 남은 동안 열 퍼센트 직접 피해 가산 확인
            Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 기존 적 행동을 실제로 소비할 적 턴 진입
            var action = new BattleEnemyAction(target, ally, EnemyActionType.Attack, BattleDamageType.Physical, 20, BattleStatusEffectType.None, 1, 1, EnemyTargetRule.FirstLiving, "서리 감옥 검사", 1, 1, 1, 0); // 원래 기획과 같은 실제 취소 대상 공격
            Assert.AreEqual(0, action.Execute().DamageResult.AppliedDamage); // 기존 경직이 원래 적 행동을 한 번 그대로 취소
            Assert.IsNull(target.GetStatusEffect(BattleStatusEffectType.Stagger)); // 원래 행동 처리로 경직과 같이 유효 출처 소멸
            Assert.AreEqual(19, target.PreviewDamage(19, BattleDamageType.Magical, ally, alliedDirectAttack: true).RawDamage); // 적 행동을 거친 뒤 집게 가산 해제
        } // 성공과 면역 및 난수 실패 대조 종료
        yield return new ExitPlayMode(); // 독립 전투와 유물 정리
    } // 서리 감옥의 실제 출처 검사 종료

    [UnityTest] // 실제 정수 난수와 원본 출혈 적용의 기계 분류 검사
    public IEnumerator ConveyorConvertsOnlyMechanicalDirectBleedWithTwentyExtraPoints() // 기계형의 두 턴 물리 약화와 일반형 출혈 대조
    { // 원래 찌르기의 대상과 확률 검사 시작
        yield return new EnterPlayMode(); // 실제 손패와 유물의 플레이 모드 준비
        using (var owned = new OwnedRelic("ConveyorTooth")) // 컨베이어 톱니의 실제 회차 장착
        { // 같은 체력의 두 적 분류 준비 시작
            var mechanical = MakeEnemy("CONVEYOR_MECHANICAL", true); // 원본에서 명시한 기계형 적
            var organic = MakeEnemy("CONVEYOR_ORGANIC", false); // 같은 방어 수치의 일반 적
            try // 테스트 원본의 확실한 해제 경계
            { // 실제 세 종류의 후속 상태와 기간 검증 시작
                using var fight = Fight.WithEnemySources(new[] { mechanical, organic, mechanical, mechanical }, "THRUST", "THRUST", "THRUST", "THRUST"); // 일반 원본 찌르기로 실제 분류와 난수 대조
                int chance = fight.View("THRUST").RuntimeCard.SourceData.SecondaryEffectValue; // 실제 원본 출혈 성공률
                Assert.Greater(chance, 0); // 출혈 적용 시도 자체가 있는 카드 원본
                Assert.Less(chance + 20, 100); // 정확한 확률 실패 경계의 재현 조건
                int seed = FindSeed(chance + 19); // 톱니에서만 성공하는 마지막 정수 난수
                fight.Controller.CardUsed += (_, __) => Random.InitState(seed); // 실제 원본 타격 직전의 정수 난수 지정
                yield return fight.Use("THRUST", 0); // 기계형 상태가 성공하는 실제 명중 공격
                var converted = fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown); // 치환 후 실제 남아 있는 상태
                Assert.IsNotNull(converted); // 원래 물리 약화의 정상 적용 확인
                Assert.AreEqual(2, converted.EffectiveValue); // 같은 유물의 실제 물리 방어 약화 값
                Assert.AreEqual(2, converted.RemainingTurns); // 적용한 물리 방어 약화의 두 적 턴
                Assert.IsNull(fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.Bleed)); // 같은 한 타격에서 출혈 중복 적용 없음
                yield return fight.Use("THRUST", 1); // 같은 난수의 일반형 적 직접 타격
                Assert.IsNull(fight.Enemies[1].GetStatusEffect(BattleStatusEffectType.Bleed)); // 일반형의 원래 확률 실패
                Assert.IsNull(fight.Enemies[1].GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown)); // 일반형의 물리 약화 치환 없음
                seed = FindSeed(chance + 20); // 톱니가 있어도 실패하는 첫 정수 난수
                yield return fight.Use("THRUST", 2); // 동일 기계형의 실제 실패 타격
                Assert.IsNull(fight.Enemies[2].GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown)); // 확률이 정확히 스무 퍼센트포인트인지 확인
                seed = FindSeed(chance - 1); // 원래 출혈도 성공할 유효 정수 난수
                yield return fight.Use("THRUST", 1); // 실제 일반형 적에게 원래 출혈 부여
                Assert.IsNotNull(fight.Enemies[1].GetStatusEffect(BattleStatusEffectType.Bleed)); // 기존 일반형 출혈 상태 유지
                fight.Enemies[3].ApplyStatusEffect(BattleStatusEffectType.Bleed, 2, 2, 1, fight.Allies[0]); // 카드 명중 밖의 독립 일반 출혈 부여
                Assert.IsNotNull(fight.Enemies[3].GetStatusEffect(BattleStatusEffectType.Bleed)); // 일반 상태 부여를 임의로 치환하지 않는 범위 확인
                Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 치환 상태의 첫 실제 대상 적 턴 처리
                Assert.AreEqual(1, converted.RemainingTurns); // 아군 종료와 별개의 적 턴 한 번 차감
                Assert.IsTrue(fight.Turns.EndPlayerTurn() && fight.Turns.CompleteEnemyTurn()); // 치환 상태의 두 번째 실제 적 턴 처리
                Assert.IsNull(fight.Enemies[0].GetStatusEffect(BattleStatusEffectType.PhysicalDefenseDown)); // 두 턴 뒤 기본 물리 방어 상태 복원
            } // 실제 세 종류의 후속 상태와 기간 검증 종료
            finally // 실제 프로젝트 원본과 별개의 검사 자료 정리
            { // 두 분류 자료 해제 시작
                Object.Destroy(mechanical); // 테스트에서 생성한 기계형 원본 제거
                Object.Destroy(organic); // 테스트에서 생성한 일반형 원본 제거
            } // 두 분류 자료 해제 종료
        } // 같은 체력의 두 적 분류 준비 종료
        yield return new ExitPlayMode(); // 실제 검사 플레이 모드 종료
    } // 원래 찌르기의 대상과 확률 검사 종료

    private static EnemyData MakeEnemy(string id, bool mechanical) // 검사에서 쓸 실제 적 데이터 생성
    { // 같은 기본 수치와 실제 기계 속성 구성 시작
        var enemy = ScriptableObject.CreateInstance<EnemyData>(); // 원래 기계형 속성 필드를 가진 타입
        JsonUtility.FromJsonOverwrite($"{{\"enemyId\":\"{id}\",\"maxHealth\":200,\"isMechanical\":{(mechanical ? "true" : "false")}}}", enemy); // 차이가 한 분류뿐인 전투 원본
        return enemy; // 구성한 실제 적 반환
    } // 같은 기본 수치와 실제 기계 속성 구성 종료

    private static int FindSeed(int roll) // 실제 Unity 확률의 정확한 정수 시드 찾기
    { // 알려진 끝점의 원래 난수 후보 검사 시작
        for (int seed = 0; seed < 10000; seed++) // 유효한 Unity 난수 초기값 범위
        { // 다음 후보의 실제 첫 정수 판정 시작
            Random.InitState(seed); // 확률을 재현할 독립 원래 난수
            if (Random.Range(0, 100) == roll) // 원하는 실제 백분율 정수 여부
            { // 정확한 시드 반환 시작
                return seed; // 정상 전투 카드에 붙일 실제 경계 시드
            } // 정확한 시드 반환 종료
        } // 다음 후보의 실제 첫 정수 판정 종료
        throw new InvalidOperationException("상태 적용 검사 확률 난수 미발견"); // 필요한 원래 판정 경계가 없는 검사 오류
    } // 알려진 끝점의 원래 난수 후보 검사 종료

    private sealed class OwnedRelic : IDisposable // 실제 유물 회차를 검사마다 복원
    { // 유물 획득과 해제 준비 시작
        private readonly RelicRunManager previous = RelicRunManager.Instance; // 원래 존재한 회차 확인
        private readonly RelicRunManager manager = RelicRunManager.EnsureInstance(); // 실제 유물 회차 관리
        private readonly RelicData relic; // 이번 검사에서 얻은 정식 유물 원본
        internal OwnedRelic(string key) // 자산의 정식 경로로 실제 획득
        { // 실물 원본 등록 시작
            relic = Resources.Load<RelicData>("Relics/" + key); // 실제 인게임 유물 자산 조회
            Assert.IsNotNull(relic, key); // 신규 유물 등록 확인
            Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(relic)); // 회차 인벤토리의 정식 획득 확인
        } // 실물 원본 등록 종료
        public void Dispose() // 이번에 얻은 유물과 임시 인스턴스 해제
        { // 실제 게임 회차 상태 복원 시작
            manager.TryRemove(relic.RelicId); // 이번 검사 유물만 제거
            if (previous == null) // 테스트에서 보관함을 새로 만든 여부
            { // 임시 객체 정리 시작
                Object.Destroy(manager.gameObject); // 검사에서 생성한 유물 관리자 제거
            } // 임시 객체 정리 종료
        } // 실제 게임 회차 상태 복원 종료
    } // 유물 획득과 해제 준비 종료
} // 전투 유물 검사 종료
