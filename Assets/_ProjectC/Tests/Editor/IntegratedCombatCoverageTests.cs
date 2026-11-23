using System; // 기존 유물 보유 복원과 실제 난수 오류 구분
using System.Collections; // 실제 카드 입력 및 Unity 상태 전환 대기
using System.Collections.Generic; // 사용한 실제 카드 속성과 유물 보유 목록
using NUnit.Framework; // 실제 체력과 행동력 및 종료 이벤트 확인
using UnityEngine; // 실제 Unity 에셋 및 무작위 시드 조회
using UnityEngine.TestTools; // 실제 검사 전투 모드
using Fight = WinterfallSwordCombatTests.Fight; // 기존 원본 손패와 전투 처리 검증 재사용
using Object = UnityEngine.Object; // 이번 검사가 소유한 Unity 객체 제거
using Random = UnityEngine.Random; // 실제 전투 굴림의 시드 제어
using static WinterfallCommonCardExpansionTests; // 기존 실제 단일 아군 선택과 손패 입력 재사용

public sealed class IntegratedCombatCoverageTests // 다섯 계열 카드와 교전 유물의 실제 통합 결과
{ // 선언한 실제 전투 범위의 통합 확인 시작
    [UnityTest] // 실제 손패에서 다섯 계열의 유료 카드와 여러 유물 조합 확인
    public IEnumerator FiveAuthoredSuitsPayOnceAndResolveNestedKillHealingBeforeVictory() // 개막 비용부터 원래 최종 승리까지 실제 손패 사용 및 처치의 통합 흐름
    { // 원본 카드와 유물의 실제 통합 검사 시작
        yield return new EnterPlayMode(); // Editor에서 실제 전투 손패와 유물 데이터가 있는 플레이 시작
        using (var relics = new OwnedRelics()) // 현재 게임의 실제 회차 보유 관리
        { // 다섯 카드와 서로 다른 시기의 유물 시험 시작
            relics.Acquire("CrackedShieldStrap"); // 이전 일차의 금 간 방패끈 첫 방어 할인
            relics.Acquire("FlameVeinDebris"); // 실제 지팡이 화상에서 생기는 별도 마법 저항 약화
            relics.Acquire("ObsidianExecutionMask"); // 첫 출혈 적의 실제 처치에서 남은 적의 여섯 피해
            relics.Acquire("HollowPredatorStomach"); // 시작 체력 지불과 각각의 실제 처치자 회복
            relics.Acquire("SwampLeechSinew"); // 정상 출혈 적 피해의 한 번 정산 회복
            relics.Acquire("WraithMistLantern"); // 원래 명중 굴림의 다섯 퍼센트포인트 감소
            var allyViews = new Dictionary<BattleUnitRuntime, BattleUnitView>(); // 실제 클릭에 사용할 각 아군의 정상 화면 저장
            using (var fight = new Fight((parent, ally) => // 기존 원본 손패와 아군의 실제 단일 대상 선택 준비
            { // 별도 아군이 고르게 되는 실제 화면 바인딩 시작
                var view = CreateView(parent, ally); // 기존 원본 검사의 실제 Unity 아군 화면 생성
                allyViews.Add(ally, view); // 원래 카드에서 클릭할 이 아군 화면 기록
                return view; // 실제 싸움의 아군 화면 유지
            }, CreateView, "GIANTS_SHIELD", "SMALL_HEAL", "FOCUS", "SMALL_FLAME", "EXECUTIONER_SWORD")) // 게임 원본 카드 환경의 다섯 계열 구성
            { // 네이티브 다섯 카드와 확정 사용 순서의 관측 시작
                var actor = fight.Allies[0]; // 현재 실제 다섯 카드의 주인
                var recipient = fight.Allies[1]; // 성배 카드가 회복시킬 다른 실제 아군
                var suits = new List<CardType>(); // 실제 결제 뒤 효과가 시작한 카드의 속성 순서
                fight.Controller.CardUsed += (card, _) => // 정상 결제와 효과 준비 뒤의 원래 카드 사용 신호
                { // 카드 계열 기록과 원래 확률 시드 설정 시작
                    suits.Add(card.CardType); // 중복된 입력으로 추가 카드 사용이 있는지 기록
                    if (card.SourceData.CardId == "CRD_SHEET_SMALL_FLAME") // 실제 지팡이 원본의 원래 25퍼센트 화상
                    { // 원본에 필요한 실제 명중과 화상 확률 설정 시작
                        Random.InitState(FindSeed(95, 25)); // 안개 명중 보정 이후와 실제 화상 선택의 난수
                    } // 원본에 필요한 실제 명중과 화상 확률 설정 종료
                    else if (card.SourceData.CardId == "CRD_SHEET_EXECUTIONER_SWORD") // 마지막 검 타격의 실제 예고 확인
                    { // 실제 안개 명중의 정상 성공 구간 준비 시작
                        Random.InitState(FindSeed(95, 100)); // 일반 검의 첫 굴림이 원래 명중 범위 안인 시드
                    } // 실제 안개 명중의 정상 성공 구간 준비 종료
                }; // 카드 계열 기록과 원래 확률 시드 설정 종료
                Assert.AreEqual(actor.MaxHealth - 5, actor.CurrentHealth); // 빈 위장이 실제 개막 시 살아 있는 시전자에게 비용 적용
                Assert.AreEqual(recipient.MaxHealth - 5, recipient.CurrentHealth); // 같은 개막의 다른 살아 있는 아군에도 비용 적용
                Assert.IsTrue(actor.TrySpendHealth(20)); // 두 번의 처치 회복과 한 번의 출혈 직접 회복을 모두 관측할 실제 부상
                int shieldPrice = fight.View("GIANTS_SHIELD").RuntimeCard.ApCost; // 첫 실제 방패의 현재 유료 비용
                Assert.AreEqual(3, shieldPrice); // 원본 네 점 중 현재 첫 방어 카드의 금 간 방패끈 한 점 할인
                yield return fight.Use("GIANTS_SHIELD"); // 실제 방패 카드 자기 대상 클릭과 정상 결제
                Assert.Greater(actor.CurrentBlock, 0); // 방패가 실제 시전자에게 방어도를 부여
                var heal = fight.View("SMALL_HEAL").RuntimeCard.SourceData; // 상점 보상에 등록된 실제 성배 원본
                yield return Play(heal, fight.Hand, allyViews[recipient], fight.Runner, fight.Deck, fight.Points); // 다른 아군을 진짜 클릭하는 성배 카드
                Assert.AreEqual(recipient.MaxHealth, recipient.CurrentHealth); // 개막에 실제로 잃은 다른 아군 체력의 원래 정상 치유
                int handBeforeFocus = fight.Deck.Hand.Count; // 보충 전 펜타클 카드가 들어 있던 실제 손패
                yield return fight.Use("FOCUS"); // 실제 펜타클 원본의 결제와 기존 사용 카드 이동
                Assert.AreEqual(handBeforeFocus, fight.Deck.Hand.Count); // 펜타클 한 장을 쓰고 실제 카드 한 장을 뽑은 손패
                var burningTarget = fight.Enemies[2]; // 마지막 두 적과 독립된 원래 마법 피격 적
                Assert.AreEqual(95, BattleHitChance.GetPercent(actor, burningTarget)); // 실제 지팡이 굴림에서도 유지되는 원본 안개 감소
                yield return fight.Use("SMALL_FLAME", 2); // 원래 실제 지팡이 명중과 일반 화상 적용
                Assert.IsNotNull(burningTarget.GetStatusEffect(BattleStatusEffectType.Burn)); // 실제 원래 카드가 유효한 화상 상태를 만든 결과
                Assert.AreEqual(5, burningTarget.GetStatusEffect(BattleStatusEffectType.FlameVeinResistanceDown)?.EffectiveValue); // 이미 성공한 화상에서만 늘어나는 별도 마법 저항 감소
                var bleeding = fight.Enemies[0]; // 검의 마지막 원래 피해가 직접 들어갈 출혈 적
                var secondary = fight.Enemies[1]; // 고정 피해의 독립 원래 처치가 연속으로 들어갈 대상
                bleeding.TakeUnblockedDamage(bleeding.CurrentHealth - 11, BattleDamageType.None, null); // 실제 검 피해 전체보다 적은 열한 체력만 남김
                secondary.TakeUnblockedDamage(secondary.CurrentHealth - 6, BattleDamageType.None, null); // 실제 독립 참수면의 여섯 피해에 맞춘 체력
                bleeding.ApplyStatusEffect(BattleStatusEffectType.Bleed, 1, 2, 1, actor); // 원래 직접 공격이 맞기 전 활성 출혈 확인
                burningTarget.TakeUnblockedDamage(burningTarget.CurrentHealth, BattleDamageType.None, null); // 실제 최종 두 적을 제외한 대조 유닛 제거
                var spare = fight.Enemies[3]; // 실제 광역에 포함시키지 않을 마지막 대조 적
                spare.TakeUnblockedDamage(spare.CurrentHealth, BattleDamageType.None, null); // 출처 없이 제거한 유닛에서 원래 위장 회복 없음
                int wounded = actor.CurrentHealth; // 마지막 실제 카드 직전의 시전자 체력
                int completedCount = 0; // 같은 카드의 정상 승리 사건이 완료된 횟수
                int capturedHealth = -1; // 원래 승리 알림에서 디스크 기록으로 갈 실제 체력
                BattleResult completed = BattleResult.None; // 완료 이벤트에 기록되는 진짜 결과
                fight.Turns.BattleCompleted += result => // 두 공격 대상의 실제 사망 뒤 원래 전투 종료 알림
                { // 완료 당시 대상 체력과 결과 저장 시작
                    completed = result; // 실제 전투 종료의 결과 값
                    capturedHealth = actor.CurrentHealth; // 유물 회복 이후에 있는 실제 시전자 체력
                    completedCount++; // 충돌이나 입력이 재진입해도 완료는 한 번이라는 기준
                }; // 완료 당시 대상 체력과 결과 저장 종료
                var sword = fight.View("EXECUTIONER_SWORD"); // 네이티브 실제 손패에서 처형자의 검 조회
                var swordInstance = sword.RuntimeCard; // 실제 결제한 같은 실물 카드의 개별 인스턴스
                int balance = fight.Points.CurrentActionPoints; // 원래 유료 검 카드가 내기 전 행동력
                int price = swordInstance.ApCost; // 현재 실제 지불할 기본 행동력
                sword.OnPointerClick(null); // 실제 원래 카드의 적 지정 단계 진입
                fight.EnemyViews[0].OnPointerClick(null); // 원래 직접 공격 대상 결정 및 결제
                fight.EnemyViews[0].OnPointerClick(null); // 연출 도중 같은 대상에 들어온 두 번째 실제 클릭
                Assert.AreEqual(balance - price, fight.Points.CurrentActionPoints); // 반복 입력에서도 같은 카드 한 번만 결제
                yield return WaitForCard(fight); // 원래 투사체와 실제 처리 및 복귀 연출 종료 대기
                Assert.IsTrue(bleeding.IsDead && secondary.IsDead); // 실제 원본 검에 맞은 적과 참수면에 맞은 두 번째 적의 전투불능
                Assert.AreEqual(balance - price + 1, fight.Points.CurrentActionPoints); // 같은 카드의 원래 실제 처치 행동력 한 점만 회복
                CollectionAssert.Contains(fight.Deck.DiscardPile, swordInstance); // 실제 원래 검 실물이 정상적으로 사용 후 더미 이동
                CollectionAssert.AreEqual(new[] { CardType.Shield, CardType.Cup, CardType.Pentacle, CardType.Wand, CardType.Sword }, suits); // 실제 다섯 원래 계열의 사용 이벤트가 정확히 한 번씩 순서대로 발생
                Assert.AreEqual(BattleResult.Victory, completed); // 실제 두 적이 쓰러진 같은 카드의 정상 승리
                Assert.AreEqual(1, completedCount); // 같은 카드의 중첩 전투불능에서도 단 한 번 결과 확정
                Assert.AreEqual(wounded + 21, capturedHealth); // 두 아군 귀속 처치의 회복 열씩과 실제 열한 출혈 피해의 한 점
                Assert.IsFalse(fight.Turns.EndPlayerTurn()); // 전투 완료 뒤 차례 입력의 실제 재진입 차단
                Assert.AreEqual(1, completedCount); // 전투 뒤 잘못된 추가 차례가 결과를 다시 실행하지 않음
            } // 네이티브 다섯 카드와 확정 사용 순서의 관측 종료
        } // 다섯 카드와 서로 다른 시기의 유물 시험 종료
        yield return new ExitPlayMode(); // 이번 실제 검사 모드와 회차 자료 정리
    } // 원본 카드와 유물의 실제 통합 검사 종료

    [UnityTest] // 같은 카드 피해 응답 중 양 진영 전멸의 결과 확정 순서
    public IEnumerator BothTeamsAtZeroWithinOnePaidCardSettleOneDefeat() // 두 실제 피해가 같은 원래 카드 안에서 종료되는 경우의 단일 결과
    { // 기존 피해 완료 신호에서 발생한 양쪽 전투불능 확인 시작
        yield return new EnterPlayMode(); // 실물 원래 손패와 실제 전투 종료 신호 준비
        using (var fight = new Fight("EXECUTIONER_SWORD")) // 같은 카드 입력 경로의 별도 단독 전투
        { // 마지막 아군과 적의 실제 동시 종료를 만들 준비
            var actor = fight.Allies[0]; // 손패를 사용할 실제 마지막 아군
            var target = fight.Enemies[0]; // 원래 카드에 맞을 실제 마지막 적
            foreach (var ally in fight.Allies) // 마지막 시전자 외의 아군 목록 순회
            { // 별도 대조 아군을 먼저 전투불능으로 변경
                if (ally != actor) // 실제 마지막 공격자만 생존
                { // 앞선 실제 전투 경과의 처리 시작
                    ally.TakeUnblockedDamage(ally.CurrentHealth, BattleDamageType.None, null); // 현재 전투에서 다른 아군을 실제로 쓰러뜨림
                } // 앞선 실제 전투 경과의 처리 종료
            } // 별도 대조 아군을 먼저 전투불능으로 변경 종료
            foreach (var enemy in fight.Enemies) // 마지막 실제 적만 체력 네 것을 남긴 상대 명단
            { // 다른 실제 적은 먼저 사망하도록 적용
                enemy.TakeUnblockedDamage(enemy == target ? enemy.CurrentHealth - 4 : enemy.CurrentHealth, BattleDamageType.None, null); // 남을 적의 실제 방어도 없는 현재 체력 구성
            } // 다른 실제 적은 먼저 사망하도록 적용 종료
            int pairedLosses = 0; // 같은 원래 카드에서 반응으로 들어온 마지막 아군의 사망 횟수
            target.DamageResolved += (source, _, damage) => // 기존 대상의 피해는 적용됐지만 원래 사망 종료 전의 실제 신호
            { // 같은 원래 카드 안에서 마지막 아군 체력 감소를 발생시키는 검사 자극
                if (source == actor && damage.WasTargetDefeated && !actor.IsDead) // 이 카드가 마지막 적을 실제로 쓰러뜨렸는지 확인
                { // 같은 처리 스택 안에서 실제 아군의 치명 피해 호출 시작
                    pairedLosses++; // 원래 종료 전에 발생한 별도 실제 체력 피해 기록
                    actor.TakeUnblockedDamage(actor.CurrentHealth, BattleDamageType.None, null); // 양쪽 체력이 동시에 영인 정상 종료 경로 호출
                } // 같은 처리 스택 안에서 실제 아군의 치명 피해 호출 종료
            }; // 같은 원래 카드 안에서 마지막 아군 체력 감소를 발생시키는 검사 자극 종료
            var results = new List<BattleResult>(); // 실제 네이티브 전투 완료 횟수와 결과 보관
            fight.Turns.BattleCompleted += result => results.Add(result); // 현재 전투의 실제 완료 알림
            var sword = fight.View("EXECUTIONER_SWORD"); // 실제 원본 카드의 손패 입력
            int balance = fight.Points.CurrentActionPoints; // 실제 최종 카드 사용의 변경 전 행동력
            int price = sword.RuntimeCard.ApCost; // 동일 실제 원본 카드의 지불 값
            sword.OnPointerClick(null); // 원래 카드 선택의 실제 UI 호출
            fight.EnemyViews[0].OnPointerClick(null); // 현재 마지막 적의 실제 원본 공격 시작
            fight.EnemyViews[0].OnPointerClick(null); // 같은 결제가 반복 입력으로 또 실행되지 않는 확인
            Assert.AreEqual(balance - price, fight.Points.CurrentActionPoints); // 양쪽 전멸을 만들기 전의 실제 단 한 번 결제
            yield return WaitForCard(fight); // 양쪽 사망 및 원래 타격 연출의 종료
            Assert.IsTrue(actor.IsDead && target.IsDead); // 단일 원래 카드 처리 도중 두 마지막 유닛 실제 전투불능
            Assert.AreEqual(1, pairedLosses); // 현재 전투에서 한 번만 발생한 중첩 실제 피해
            CollectionAssert.AreEqual(new[] { BattleResult.Defeat }, results); // 실제 양측 전멸은 승리 추가 없이 한 번의 패배
            Assert.IsFalse(fight.Turns.EndPlayerTurn()); // 끝난 패배 뒤 같은 원래 입력 정산 차단
            Assert.AreEqual(1, results.Count); // 끝난 교전의 결과 재발행 방지
        } // 마지막 아군과 적의 실제 동시 종료를 만들 준비 종료
        yield return new ExitPlayMode(); // 이 실제 검사 전투 모드 정리
    } // 기존 피해 완료 신호에서 발생한 양쪽 전투불능 확인 종료

    private static IEnumerator WaitForCard(Fight fight) // 현재 실제 카드가 완료 영상과 피해 후 정산을 마칠 때까지 대기
    { // 게임의 원래 네이티브 상태 대기 시작
        float until = Time.realtimeSinceStartup + 10f; // 기존 손패 검사와 같은 실제 입력 종료 제한
        while (fight.Runner.IsBusy && Time.realtimeSinceStartup < until) // 현재 정상 카드 완료 대기
        { // 같은 Unity 프레임마다 원래 상태를 확인
            yield return null; // 테스트가 종료 이벤트를 기다리는 다음 프레임
        } // 같은 Unity 프레임마다 원래 상태를 확인 종료
        Assert.IsFalse(fight.Runner.IsBusy); // 현재 실제 카드 연출이 제한 안에 정상 종료
    } // 게임의 원래 네이티브 상태 대기 종료

    private static int FindSeed(int hitChance, int statusChance) // 실제 Unity에서 독립 명중과 후속 상태를 모두 만족하는 원래 난수
    { // 실제 상태별 결정적 시드 조회 시작
        var current = Random.state; // 다른 사례가 사용 중인 실제 Unity 난수 복구 자료
        try // 실제 시드 탐색 뒤 이전 값을 안전하게 복구
        { // 범위 안의 실제 게임 첫 두 굴림 검색 시작
            for (int seed = 0; seed < 10000; seed++) // 정해진 후보 안의 실제 난수 시드 확인
            { // 실제 원래 난수 초기화와 원하는 백분율 시험 시작
                Random.InitState(seed); // 현재 네이티브 공격의 초기 난수 후보
                if (Random.Range(0, 100) < hitChance && Random.Range(0, 100) < statusChance) // 실제 명중과 원래 상태 적용 범위
                { // 정확히 성공하는 시드 반환
                    return seed; // 현재 두 독립 판정에 전달할 원래 실제 시드
                } // 정확히 성공하는 시드 반환 종료
            } // 실제 원래 난수 초기화와 원하는 백분율 시험 종료
            throw new InvalidOperationException("실제 명중과 화상에 맞는 시드 없음"); // 정해진 범위 안에 결과가 없는 원래 검사 실패
        } // 범위 안의 실제 게임 첫 두 굴림 검색 종료
        finally // 실제 게임 밖의 다른 범위에 난수 상태를 남기지 않는 복구
        { // 이전 상태의 기존 실제값 적용
            Random.state = current; // 다른 네이티브 판정에 영향을 주지 않는 원래 상태
        } // 이전 상태의 기존 실제값 적용 종료
    } // 실제 상태별 결정적 시드 조회 종료

    private sealed class OwnedRelics : IDisposable // 같은 실제 회차에 유물 여러 개를 등록하고 제거하는 검사
    { // 이전과 현재의 보유 자료를 구분하는 실제 기록
        private readonly RelicRunManager prior = RelicRunManager.Instance; // 이미 실제 회차에 있던 원래 보유 관리자
        private readonly RelicRunManager manager = RelicRunManager.EnsureInstance(); // 기존 게임과 같은 실제 보유 데이터
        private readonly List<RelicData> acquired = new List<RelicData>(); // 이 실제 검사에서 획득한 유물만 보관
        public void Acquire(string key) // 실제 자원 루트에서 원래 씬의 네이티브 유물 획득
        { // 정식 원본이 있는 유물만 현재 런에 장착
            var asset = Resources.Load<RelicData>("Relics/" + key); // 실제 리소스 폴더의 원래 유물
            Assert.IsNotNull(asset, key); // 전투에서 실제로 불러올 수 있는지 확인
            Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(asset), key); // 현재 런에 실제 장착된 원본만 검증
            acquired.Add(asset); // 끝나면 실제로 제거할 이번 유물 기록
        } // 정식 원본이 있는 유물만 현재 런에 장착 종료
        public void Dispose() // 이전 런의 실제 소유를 건드리지 않고 독립 사례 제거
        { // 이번 검사에서 가져온 것만 정리 시작
            foreach (var asset in acquired) // 실제로 확보된 게임 원본의 목록
            { // 정상 원래 보유 상태에서 사례 유물 제거
                manager.TryRemove(asset.RelicId); // 기존 말소 함수로 이번 유물만 해제
            } // 정상 원래 보유 상태에서 사례 유물 제거 종료
            if (prior == null) // 실제 이전 회차 객체가 없었던 경우 확인
            { // 이번에 만든 관리 객체 종료
                Object.Destroy(manager.gameObject); // 임시 실제 Unity 관리 객체만 제거
            } // 이번에 만든 관리 객체 종료
        } // 이번 검사에서 가져온 것만 정리 종료
    } // 이전과 현재의 보유 자료를 구분하는 실제 기록 종료
} // 선언한 실제 전투 범위의 통합 확인 종료
