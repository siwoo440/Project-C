using System.Collections; // 실제 카드 연출과 플레이 모드 전환 대기
using System.Linq; // 현재 전투 화면의 아군과 선택 안내 조회
using NUnit.Framework; // 두 대상과 복사 결과 검증
using TMPro; // 손패 위의 실제 대상 선택 문구 조회
using UnityEditor; // 테스트에서 실제 전투 유닛 프리팹 조회
using UnityEngine; // 유닛 원본과 화면 연결
using UnityEngine.EventSystems; // 실제 카드 드래그 입력 생성
using UnityEngine.TestTools; // 플레이 모드에서 사용하는 회귀 검사
using UnityEngine.UI; // 실제 선택 윤곽선 상태 확인
using Fight = WinterfallSwordCombatTests.Fight; // 공통 전투 항목과 실제 카드 사용 환경

public sealed class WinterfallBlessingTransferTests // 축복 이전의 실제 두 대상과 강화 복사 검사
{ // 축복 이전 검사 시작
    [UnityTest] // 서로 다른 아군의 클릭과 상태 보존 검사
    public IEnumerator CopiesFirstEligibleBlessingAndPreservesSourceAndLifetime() // 먼저 적용한 일반 강화의 원래 중첩과 수명 확인
    { // 실제 축복 입력 검사 시작
        yield return new EnterPlayMode(); // 실제 카드 환경 초기화 완료 대기
        yield return VerifyClick(); // 카드 클릭과 강화 복사 검증 수행
        yield return new ExitPlayMode(); // 실제 전투 모드 종료 및 에디터 복귀
    } // 실제 축복 입력 검사 종료

    private static IEnumerator VerifyClick() // 실제 손패와 두 아군의 클릭 입력 확인
    { // 클릭 회귀 검사 시작
        using var fight = new Fight(CreateView, CreateView, "BLESSING_TRANSFER"); // 새 축복 원본과 실제 대상 프리팹 준비
        var card = fight.View("BLESSING_TRANSFER"); // 실제 손패의 축복 이전 카드 조회
        var used = card.RuntimeCard; // 비용과 버린 더미를 추적할 실제 카드 참조
        var source = fight.Allies[1]; // 복사할 강화의 원래 보유 아군
        var recipient = fight.Allies[2]; // 다른 아군인 실제 수령자
        var sourceView = FindView(fight, source); // 강화 보유 아군의 실제 클릭 화면
        var recipientView = FindView(fight, recipient); // 강화 수령 아군의 실제 클릭 화면
        int points = fight.Points.CurrentActionPoints; // 대상 선택 전 공용 행동력 보관
        int cost = used.ApCost; // 현재 원본의 실제 지불 비용 보관
        source.ApplyStatusEffect(BattleStatusEffectType.DamageNullification, 1, 1, 1); // 복사에서 제외할 먼저 적용된 일회성 보호
        card.OnPointerClick(null); // 일회성 보호만 있는 상태의 카드 선택 시도
        Assert.IsNull(fight.Controller.SelectedCard); // 복사 가능한 강화가 없는 카드 선택 차단 확인
        source.ApplyStatusEffect(BattleStatusEffectType.AttackPowerUp, 3, 4, 4); // 첫 복사 가능 강화의 실제 적용
        source.ApplyStatusEffect(BattleStatusEffectType.AttackPowerUp, 3, 4, 4); // 두 번째 중첩을 실제로 적용
        source.ApplyStatusEffect(BattleStatusEffectType.ResistanceStance, 5, 2, 1); // 더 늦게 적용된 별도의 마법 저항 강화
        recipient.ApplyStatusEffect(BattleStatusEffectType.StatusImmunity, 1, 2, 1); // 일반 아군 강화만은 허용하는 디버프 면역 부여
        var original = source.GetStatusEffect(BattleStatusEffectType.AttackPowerUp); // 원래 강화 인스턴스 조회
        original.ConsumeDurationDecreaseSkip(); // 지난 턴에 이미 소비한 첫 감소 예약 재현
        original.AdvanceDuration(); // 적용 직후보다 짧아진 실제 남은 시간 재현
        card.OnPointerClick(null); // 실제 손패 카드 선택 시작
        StringAssert.Contains("강화를 복사할 아군", Prompt(fight)); // 첫 번째 유닛 선택 안내 확인
        Assert.IsTrue(IsOutlined(sourceView)); // 원본 강화 보유자만 고를 수 있는 실제 윤곽선 확인
        Assert.IsFalse(IsOutlined(fight.EnemyViews[0])); // 첫 단계에서 적 대상을 강조하지 않음 확인
        sourceView.OnPointerClick(null); // 현재 강화를 가진 아군을 첫 대상으로 선택
        StringAssert.Contains("강화를 받을 아군", Prompt(fight)); // 수령자 선택 단계의 실제 안내 확인
        Assert.IsFalse(IsOutlined(sourceView)); // 이미 고른 아군의 두 번째 대상 강조 제외 확인
        Assert.IsTrue(IsOutlined(recipientView)); // 수령 가능한 다른 아군의 실제 윤곽선 확인
        sourceView.OnPointerClick(null); // 같은 아군을 두 번째 대상으로 잘못 선택
        Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 같은 아군 중복 입력의 비용 보존 확인
        Assert.IsNull(recipient.GetStatusEffect(BattleStatusEffectType.AttackPowerUp)); // 첫 선택만으로 효과가 적용되지 않음 확인
        recipientView.OnPointerClick(null); // 다른 생존 아군을 수령자로 선택하여 실행
        Assert.AreEqual(points - cost, fight.Points.CurrentActionPoints); // 두 아군 확인 후 한 번만 결제
        yield return WaitForImpact(fight); // 실제 카드 적용과 손패 이동까지 대기
        CollectionAssert.Contains(fight.Deck.DiscardPile, used); // 정상 카드 사용의 버린 더미 이동 확인
        var copy = recipient.GetStatusEffect(BattleStatusEffectType.AttackPowerUp); // 실제로 복제된 강화 인스턴스 조회
        Assert.IsNotNull(copy); // 상태 면역이 있어도 실제 긍정 상태 부여 확인
        Assert.AreNotSame(original, copy); // 두 아군이 서로 다른 강화 개체를 보유하는지 확인
        Assert.AreSame(original, source.GetStatusEffect(BattleStatusEffectType.AttackPowerUp)); // 원래 강화 개체 유지 확인
        Assert.AreEqual(3, copy.Value); // 부여된 원래 중첩당 수치 보존 확인
        Assert.AreEqual(2, copy.StackCount); // 원래 두 중첩 그대로 복사 확인
        Assert.AreEqual(4, copy.MaximumStacks); // 원본의 최대 중첩 한도 복사 확인
        Assert.AreEqual(3, copy.RemainingTurns); // 감소가 끝난 원본의 실제 남은 시간 복사 확인
        Assert.IsNull(recipient.GetStatusEffect(BattleStatusEffectType.DamageNullification)); // 먼저 적용한 일회성 보호 복사 제외 확인
        Assert.IsNull(recipient.GetStatusEffect(BattleStatusEffectType.ResistanceStance)); // 나중에 부여된 두 번째 강화 복사 제외 확인
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 원본과 복제본의 실제 지속 처리 진행
        Assert.AreEqual(2, original.RemainingTurns); // 원본 강화의 기존 시간 진행 확인
        Assert.AreEqual(original.RemainingTurns, copy.RemainingTurns); // 새 예약 없이 두 강화의 동일한 시간 진행 확인
    } // 클릭 회귀 검사 종료

    [UnityTest] // 드래그 순서와 무효화 및 기존 중첩 검사
    public IEnumerator DragCancelsInvalidTransfersAndMergesOnlyWithEnoughCapacity() // 무효 드롭 취소와 같은 강화의 호환 중첩 검증
    { // 드래그 검사 모드 시작
        yield return new EnterPlayMode(); // 실제 드래그 환경 초기화 완료 대기
        yield return VerifyDrag(); // 취소와 재선택 및 마법 저항 증폭 확인
        yield return new ExitPlayMode(); // 전투 환경 해제와 에디터 복귀
    } // 드래그 검사 모드 종료

    private static IEnumerator VerifyDrag() // 실제 드롭과 클릭을 섞은 단계의 정산 검사
    { // 드래그 회귀 검사 시작
        using var fight = new Fight(CreateView, CreateView, "BLESSING_TRANSFER"); // 실제 축복 원본 한 장과 전투 화면 준비
        var card = fight.View("BLESSING_TRANSFER"); // 실제 드래그할 카드 조회
        var source = fight.Allies[1]; // 강화가 없어질 수 있는 실제 첫 아군
        var recipient = fight.Allies[2]; // 같은 강화의 기존 중첩을 가진 수령자
        var enemy = fight.Enemies[0]; // 같은 실제 공격자를 써서 원본 방어 기준 측정
        int baselinePhysical = recipient.PreviewDamage(100, BattleDamageType.Physical, enemy, enemyDirectAttack: true).FinalDamage; // 강화가 없는 기본 물리 피해
        int baselineMagic = recipient.PreviewDamage(100, BattleDamageType.Magical, enemy, enemyDirectAttack: true).FinalDamage; // 강화가 없는 기본 마법 피해
        source.ApplyStatusEffect(BattleStatusEffectType.ResistanceStance, 5, 2, 1); // 복사할 마법 전용 저항 부여
        recipient.ApplyStatusEffect(BattleStatusEffectType.ResistanceStance, 2, 1, 1); // 초기 중첩 한도를 이미 채운 수령자
        var sourceView = FindView(fight, source); // 드래그로 가리킬 첫 아군 화면
        var recipientView = FindView(fight, recipient); // 드래그로 가리킬 수령자 화면
        var drag = card.GetComponent<BattleCardDragHandler>() ?? card.gameObject.AddComponent<BattleCardDragHandler>(); // 기존 게임 드래그 입력 처리기 확보
        drag.Initialize(card); // 실제 카드 화면과 드래그 입력 연결
        var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }; // 실제 왼쪽 버튼 드롭 입력 준비
        int points = fight.Points.CurrentActionPoints; // 드롭 취소 전의 공용 행동력 보관
        int cost = card.RuntimeCard.ApCost; // 성공한 사용에만 내는 원본 비용 보관
        Drop(drag, pointer, sourceView); // 첫 드롭으로 강화 보유자 선택
        StringAssert.Contains("강화를 받을 아군", Prompt(fight)); // 드래그의 실제 두 번째 안내 확인
        Drop(drag, pointer, recipientView); // 활성 중첩이 가득 찬 수령자에 무효 드롭
        Assert.IsNull(fight.Controller.SelectedCard); // 잘못된 두 번째 드롭의 최초 선택 정리 확인
        Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 무효 드롭이 공용 행동력을 쓰지 않음 확인
        Drop(drag, pointer, sourceView); // 취소 뒤 처음부터 같은 유효 보유자 선택
        source.RemoveStatusEffect(BattleStatusEffectType.ResistanceStance); // 첫 선택 이후 실제 강화 해제로 원본 무효화
        Assert.IsNull(fight.Controller.SelectedCard); // 원본 강화가 사라지면 두 번째 입력 전에 취소 확인
        Assert.AreEqual(points, fight.Points.CurrentActionPoints); // 강화를 잃은 미완료 선택의 결제 차단 확인
        source.ApplyStatusEffect(BattleStatusEffectType.ResistanceStance, 5, 2, 2); // 기존 수령자와 합산할 수 있는 중첩 한도의 강화 재적용
        card.OnPointerClick(null); // 실제 손패 클릭으로 새 선택 시작
        sourceView.OnPointerClick(null); // 첫 번째 아군을 일반 클릭으로 선택
        Drop(drag, pointer, recipientView); // 수령자를 실제 드래그로 확정
        Assert.AreEqual(points - cost, fight.Points.CurrentActionPoints); // 최종 두 단계 성공의 한 번 비용 확인
        yield return WaitForImpact(fight); // 실제 복사 적용 대기
        var stance = recipient.GetStatusEffect(BattleStatusEffectType.ResistanceStance); // 수령자의 기존 상태에 합쳐진 마법 저항 조회
        Assert.AreEqual(2, stance.StackCount); // 원래 한 중첩에 복사한 한 중첩 추가 확인
        Assert.AreEqual(10, stance.EffectiveValue); // 기존 중첩 규칙의 높은 수치로 두 중첩 합산 확인
        Assert.AreEqual(5, source.GetStatusEffect(BattleStatusEffectType.ResistanceStance).EffectiveValue); // 강화 보유자의 원본 수치 유지 확인
        Assert.Less(recipient.PreviewDamage(100, BattleDamageType.Magical, enemy, enemyDirectAttack: true).FinalDamage, baselineMagic); // 같은 기본 방어 위 마법 피해만 추가로 감소
        Assert.AreEqual(baselinePhysical, recipient.PreviewDamage(100, BattleDamageType.Physical, enemy, enemyDirectAttack: true).FinalDamage); // 복사 전후 물리 피해의 동일한 실제 수치
    } // 드래그 회귀 검사 종료

    private static BattleUnitView CreateView(Transform parent, BattleUnitRuntime unit) // 실제 선택 윤곽선이 있는 유닛 화면 생성
    { // 전투 프리팹 연결 시작
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ProjectC/Prefabs/Common/PF_BattleUnit.prefab"); // 실제 실행 화면의 전투 유닛 원본 조회
        var view = Object.Instantiate(prefab, parent).GetComponent<BattleUnitView>(); // 전투 화면에 원본 연결
        view.Bind(unit); // 실제 상태와 입력 대상 조합 연결
        return view; // 회귀 검사의 실제 유닛 화면 반환
    } // 전투 프리팹 연결 종료

    private static BattleUnitView FindView(Fight fight, BattleUnitRuntime unit) // 지정 아군의 실제 화면 찾기
    { // 유닛 화면 조회 시작
        return fight.Hand.transform.parent.GetComponentsInChildren<BattleUnitView>().Single(view => view.RuntimeUnit == unit); // 현재 참가자의 실제 클릭 화면 반환
    } // 유닛 화면 조회 종료

    private static bool IsOutlined(BattleUnitView view) // 실제 대상 후보 윤곽선 확인
    { // 윤곽선 조회 시작
        return view.GetComponentsInChildren<Outline>().Any(outline => outline.enabled); // 유효한 선택 후보 표시 여부 반환
    } // 윤곽선 조회 종료

    private static string Prompt(Fight fight) // 현재 두 단계 선택의 실제 안내 조회
    { // 화면 안내 조회 시작
        return fight.Hand.transform.parent.GetComponentsInChildren<TMP_Text>(true).Single(label => label.name == "TurnStatusText").text; // 실제 손패의 안내 텍스트 반환
    } // 화면 안내 조회 종료

    private static void Drop(BattleCardDragHandler drag, PointerEventData pointer, BattleUnitView target) // 현재 카드의 실제 드래그와 화면 드롭 실행
    { // 유닛 드롭 실행 시작
        drag.OnBeginDrag(pointer); // 현재 선택 단계에서 드래그 시작
        pointer.pointerCurrentRaycast = new RaycastResult { gameObject = target.gameObject }; // 유닛 화면을 드롭 위의 대상에 설정
        drag.OnEndDrag(pointer); // 실제 카드 드롭의 유효 대상과 사용 처리 실행
    } // 유닛 드롭 실행 종료

    private static IEnumerator WaitForImpact(Fight fight) // 실제 연출과 효과 정산 종료 대기
    { // 연출 완료 대기 시작
        float deadline = Time.realtimeSinceStartup + 10f; // 전투 연출의 최대 대기 시간 설정
        while (fight.Runner.IsBusy && Time.realtimeSinceStartup < deadline) // 카드 적용 뒤 입력 복구까지 확인
        { // 다음 화면 대기 시작
            yield return null; // 실제 렌더링 프레임 한 번 대기
        } // 다음 화면 대기 종료
        Assert.IsFalse(fight.Runner.IsBusy); // 유한 시간 안에 카드 결산 완료 확인
    } // 연출 완료 대기 종료
} // 축복 이전 검사 종료
