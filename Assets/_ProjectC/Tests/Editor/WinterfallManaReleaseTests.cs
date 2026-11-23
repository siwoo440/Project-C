using System.Collections; // 실제 카드 연출과 플레이 모드 전환 대기
using System.Linq; // 아군의 실제 선택 화면 조회
using NUnit.Framework; // 행동력과 피해 및 카드 표시 검증
using TMPro; // 실제 툴팁과 카드 비용 텍스트 조회
using UnityEngine; // 실전 화면과 프레임 대기 시간 사용
using UnityEngine.TestTools; // 도메인 전환과 전투 입력 검사
using Fight = WinterfallSwordCombatTests.Fight; // 기존 손패와 대상 클릭이 연결된 전투 검사 환경

public sealed class WinterfallManaReleaseTests // 마력 해방의 가변 비용과 실제 단일 마법 피해 검사
{ // 마력 해방 검사 시작
    [UnityTest] // 실제 행동력 표시와 정상 및 취소 입력 검증
    public IEnumerator CastUsesActualConfirmedActionPointsAndOnlyHitsChosenEnemy() // 대상 확정 전 보존과 실제 지불량 기준 타격 검증
    { // 실제 플레이 모드 검사 시작
        yield return new EnterPlayMode(); // 원본 손패와 카드 연출이 활성화되는 검사 모드 진입
        yield return VerifyCastAndCancel(); // 가변 비용의 표시와 시점 및 실제 피해 결과 검사
        yield return new ExitPlayMode(); // 전투 검사 환경을 정리한 뒤 편집 모드 복귀
    } // 실제 플레이 모드 검사 종료

    private static IEnumerator VerifyCastAndCancel() // 원본 마력 해방의 선택과 지불 및 명중 수치 검증
    { // 실제 카드 조작 검증 시작
        using var fight = new Fight("MANA_RELEASE"); // 실물 원본의 지급 식별자로 카드 한 장 준비
        var owner = fight.Allies[0]; // 마력 해방을 사용하는 실제 소유자
        Assert.IsTrue(owner.ApplyPersistentMental(50)); // 일반 정신 상태의 기본 마법 피해 비교 조건 설정
        owner.ApplyStatusEffect(BattleStatusEffectType.AttackPowerUp, 5, 3, 1); // 가변 원본의 고정 배수와 일반 공격 가산 구분
        var enemy = fight.Enemies[1]; // 실제 대상 클릭으로 고를 생존 적 한 명
        enemy.GainBlock(2); // 마법 원본 피해와 흡수 후 피해를 비교할 방어도 준비
        fight.Enemies[2].TakeDamage(1000, BattleDamageType.None); // 같은 진영이지만 대상이 될 수 없는 전투불능 적 준비
        var other = fight.Enemies[0]; // 단일 목표에서 제외되는 다른 생존 적
        int otherHealth = other.CurrentHealth; // 다른 적의 실제 비교 체력
        int initialHealth = enemy.CurrentHealth; // 실제 지정 적의 피격 전 체력
        var cardView = fight.View("MANA_RELEASE"); // 손패에 표시된 원본 카드의 실제 입력 객체
        var card = cardView.RuntimeCard; // 취소와 정산 뒤까지 일치 여부를 볼 개별 카드
        Assert.AreEqual("X", card.CostLabel); // 기본 비용 영과 구별되는 가변 비용 표시 확인
        Assert.AreEqual(CardTargetType.SingleEnemy, card.TargetType); // 원본 시트의 단일 적 지정 확인
        Assert.AreEqual(BattleDamageType.Magical, card.DamageType); // 원본의 마법 피해 분류 확인
        Assert.IsTrue(fight.Points.Spend(15)); // 남은 행동력 5에 카드 미리보기를 맞출 검사 상태 준비
        cardView.OnPointerEnter(null); // 실제 손패 가리키기 입력을 기록해 행동력 변경과 직접 연결
        var tooltip = fight.Hand.transform.Find("CardTooltip").GetComponent<BattleCardTooltipView>(); // 실제 전투 손패의 원래 카드 상세 객체 조회
        tooltip.Show(card, fight.Points.CurrentActionPoints); // 실제 가변 행동력을 넣는 툴팁 표시 진입점 사용
        var detail = tooltip.transform.Find("DetailText").GetComponent<TMP_Text>(); // 표시된 카드 상세 문구 조회
        StringAssert.Contains("AP X", detail.text); // 툴팁의 실제 행동력 기호 확인
        StringAssert.Contains("마법 피해 X × 7", detail.text); // 원본 가변 마법 피해 계수 표시 확인
        StringAssert.Contains("현재 기본 피해 35", detail.text); // 남은 행동력 5의 기본 피해 미리보기 확인
        cardView.OnPointerClick(null); // 실제 손패의 마력 해방을 선택만 한 상태
        Assert.AreSame(card, fight.Controller.SelectedCard); // 단일 적을 기다리는 실제 선택 상태 확인
        Assert.AreEqual(5, fight.Points.CurrentActionPoints); // 선택만으로 행동력이 줄지 않는지 확인
        var invalid = fight.Hand.GetComponentInParent<Canvas>().GetComponentsInChildren<BattleUnitView>().Single(view => view.RuntimeUnit == fight.Allies[1]); // 적이 아닌 생존 아군의 클릭 화면 조회
        invalid.OnPointerClick(null); // 허용되지 않는 아군 클릭 입력 실행
        Assert.AreSame(card, fight.Controller.SelectedCard); // 잘못 누른 적이 아닌 대상이 지불을 확정하지 않음 확인
        Assert.AreEqual(5, fight.Points.CurrentActionPoints); // 잘못된 대상 클릭에 행동력 유지 확인
        fight.EnemyViews[2].OnPointerClick(null); // 전투불능 적의 위치도 실제 대상으로 선택할 수 없는지 확인
        Assert.AreSame(card, fight.Controller.SelectedCard); // 전투불능 대상 입력 뒤 생존 적 선택 단계 보존 확인
        Assert.AreEqual(5, fight.Points.CurrentActionPoints); // 잘못 선택한 쓰러진 대상에 행동력 보존 확인
        cardView.OnPointerClick(null); // 같은 카드 재선택으로 실제 취소 입력 실행
        Assert.IsNull(fight.Controller.SelectedCard); // 카드와 대상 대기 상태의 정상 취소 확인
        Assert.AreEqual(5, fight.Points.CurrentActionPoints); // 취소한 경우에도 행동력을 유지하는지 확인
        CollectionAssert.Contains(fight.Deck.Hand, card); // 사용하지 않은 개별 카드가 손패에 남는지 확인
        cardView.OnPointerClick(null); // 동일 카드의 새 타격 대상 선택 준비
        Assert.IsTrue(fight.Points.Spend(1)); // 선택과 대상 확정 사이에 실제 행동력이 4로 줄어드는 상황 적용
        StringAssert.Contains("현재 기본 피해 28", detail.text); // 보이는 미리보기의 최신 실제 행동력 즉시 반영 확인
        BattleDamageResult observed = default; // 실제 피격 이벤트로 관측할 최종 계산 기록
        int hits = 0; // 대상 한 명의 실제 피격 처리 횟수
        enemy.DamageTaken += (_, damage) => // 확인한 적의 실제 피해 기록 구독
        { // 피격 결과 보관 시작
            observed = damage; // 원본 피해와 흡수량 및 실제 피해 결과 기록
            hits++; // 한 번의 최초 타격 외 중복 처리 확인
        }; // 피격 결과 보관 종료
        int depleted = 0; // 모두 지불한 즉시 행동력 변경의 실제 호출 수
        fight.Points.StateChanged += () => // 소모 직후 다른 효과가 행동력을 바꾸는 상황의 상태 알림 구독
        { // 행동력 지불 알림 검사 시작
            if (fight.Points.CurrentActionPoints == 0) // 정산 시점의 한 번 전액 지불한 상태 감지
            { // 실제 소비 이후 행동력 수량 변경 시작
                depleted++; // 직전 행동력이 전액 차감된 횟수 기록
                fight.Points.Gain(2); // 사용 이후의 행동력으로 지불량을 덮어쓰지 않는지 비교
            } // 실제 소비 이후 행동력 수량 변경 종료
        }; // 행동력 지불 알림 검사 종료
        fight.EnemyViews[1].OnPointerClick(null); // 정확한 지정 적에게 마력 해방 타격 확정
        Assert.AreEqual(1, depleted); // 실제 남은 네 점을 한 번만 모두 지불 확인
        Assert.AreEqual(2, fight.Points.CurrentActionPoints); // 별개의 행동력 회복과 실제 지불량 구분 확인
        float deadline = Time.realtimeSinceStartup + 10f; // 카드의 실제 연출과 정산을 기다릴 상한
        while (fight.Runner.IsBusy && Time.realtimeSinceStartup < deadline) // 기존 카드 연출이 끝날 때까지 대기
        { // 실제 전투 프레임 대기 시작
            yield return null; // 다음 엔진 화면과 카드 정산 기회 대기
        } // 실제 전투 프레임 대기 종료
        Assert.IsFalse(fight.Runner.IsBusy); // 타격과 복귀 연출의 정상 종료 확인
        Assert.AreEqual(1, hits); // 지정한 적에게 한 번만 마법 피해가 적용됐는지 확인
        Assert.AreEqual(28, observed.RawDamage); // 실제 지불한 네 점에 배수 일곱 적용 확인
        Assert.AreEqual(BattleDamageType.Magical, observed.DamageType); // 실제 마법 피해 경로의 계산 결과 확인
        Assert.AreEqual(2, observed.AbsorbedDamage); // 마법 원본과 별개인 실제 방어도 흡수 확인
        Assert.AreEqual(initialHealth - 26, enemy.CurrentHealth); // 방어도 차감 이후의 실제 대상 체력 검증
        Assert.AreEqual(otherHealth, other.CurrentHealth); // 지정하지 않은 다른 생존 적의 체력 보존 확인
        CollectionAssert.Contains(fight.Deck.DiscardPile, card); // 사용한 정확한 개별 카드 한 장의 정상 정산 확인
    } // 실제 카드 조작 검증 종료

    [UnityTest] // 행동력 영에서 대상 선택과 비용 지불이 차단되는 실제 경로 검사
    public IEnumerator ZeroActionPointsLeaveManaReleaseInHandWithoutTargeting() // 행동력이 없는 상태의 일반 입력을 차단하는지 확인
    { // 영 행동력 검사 시작
        yield return new EnterPlayMode(); // 실제 손패 검사 초기화 대기
        VerifyZeroCostGate(); // 비용과 손패 및 적 체력을 동시에 확인
        yield return new ExitPlayMode(); // 격리 전투의 검사 종료
    } // 영 행동력 검사 종료

    private static void VerifyZeroCostGate() // 행동력 영의 실제 손패 사용 불가 검사
    { // 손패 상태 검증 시작
        using var fight = new Fight("MANA_RELEASE"); // 원본의 가변 카드로 독립 회차 진행
        var view = fight.View("MANA_RELEASE"); // 실제 손패 카드의 클릭 입력 객체
        var used = view.RuntimeCard; // 손패와 버린 더미의 개별 카드 비교 자료
        view.OnPointerClick(null); // 행동력이 있을 때 카드 선택 단계까지 실제 진행
        Assert.AreSame(used, fight.Controller.SelectedCard); // 대상 입력을 기다리는 선택 상태 확인
        Assert.IsTrue(fight.Points.Spend(fight.Points.CurrentActionPoints)); // 기존 선택 뒤 행동력이 모두 사라진 상태 생성
        fight.EnemyViews[0].OnPointerClick(null); // 확정 시의 행동력 영으로 원래 선택 사용 차단 확인
        Assert.IsNull(fight.Controller.SelectedCard); // 비용 재검사에 실패한 기존 선택 상태 정리 확인
        view.OnPointerClick(null); // 가변 카드의 실제 정상 선택 입력 시도
        fight.EnemyViews[0].OnPointerClick(null); // 생존 적을 눌러도 정상 타격 입력이 되지 않는지 확인
        Assert.IsNull(fight.Controller.SelectedCard); // 행동력 영에서 대상 선택 단계 미진입 확인
        Assert.IsFalse(fight.Runner.IsBusy); // 사용 불가 카드의 실제 연출 미실행 확인
        CollectionAssert.Contains(fight.Deck.Hand, used); // 행동력 부족 시 사용하지 않은 원본 카드 유지 확인
        Assert.AreEqual(fight.Enemies[0].MaxHealth, fight.Enemies[0].CurrentHealth); // 비용 지불 없이 적 체력을 바꾸지 않음 확인
    } // 손패 상태 검증 종료
} // 마력 해방 검사 종료
