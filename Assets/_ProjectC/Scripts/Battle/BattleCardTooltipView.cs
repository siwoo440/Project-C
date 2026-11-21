using TMPro; // 텍스트 메시 기능 사용
using UnityEngine; // 유니티 기본 기능 사용
using UnityEngine.UI; // 유니티 UI 기능 사용
public sealed class BattleCardTooltipView : MonoBehaviour // 카드 상세 툴팁 화면
{ // 클래스 시작
    private TMP_Text titleText; // 카드 이름 텍스트
    private TMP_Text detailText; // 카드 상세 정보 텍스트
    private bool visualStructureCreated; // 화면 구조 생성 여부
    private void Awake() // 툴팁 화면 준비
    { // 화면 준비 시작
        EnsureVisualStructure(); // 툴팁 내부 UI 자동 생성
        Hide(); // 시작 툴팁 숨김
    } // 화면 준비 종료
    public void Show(CardInstance cardInstance) // 카드 상세 정보 표시
    { // 툴팁 표시 시작
        if (cardInstance == null) // 카드 연결 여부 확인
        { // 카드 누락 처리 시작
            Hide(); // 툴팁 숨김
            return; // 표시 처리 중단
        } // 카드 누락 처리 종료
        EnsureVisualStructure(); // 툴팁 내부 UI 확인
        titleText.text = cardInstance.DisplayName; // 카드 이름 표시
        string targetLabel = cardInstance.SourceData.SwordRule switch // 아군 또는 적의 실제 카드 대상 표시
        { // 검 카드 범위 분기 시작
            SwordCardRule.SwordDance or SwordCardRule.HorizontalSlash => "선택한 적과 인접한 한 칸", // 이웃 칸 공격 범위
            SwordCardRule.TwinBladeFlurry => "타격마다 무작위 생존 적 하나", // 전체 적에서 하나씩 고르는 연타 범위
            _ => GetTargetLabel(cardInstance) // 일반 카드와 두 아군 선택의 대상 문구
        }; // 카드 대상 표시 완료
        string effectDetail = GetEffectDetail(cardInstance) + GetFollowUpDetail(cardInstance.SourceData); // 기본 효과와 후속 효과의 대상 및 수치 표시
        detailText.text = $"소유자 {cardInstance.OwnerUnit.DisplayName}  |  AP {cardInstance.CostLabel}  |  대상 {targetLabel}\n효과 {effectDetail}\n{cardInstance.SourceData.Description}"; // 카드 상세 정보 표시
        gameObject.SetActive(true); // 툴팁 오브젝트 표시
        transform.SetAsLastSibling(); // 툴팁 최상단 표시
    } // 툴팁 표시 종료
    private static string GetFollowUpDetail(CardData source) // 원본에 설정한 후속 효과의 표시 문구
    { // 후속 효과 표시 시작
        int value = source.SecondaryEffectValue; // 원본 후속 수치 조회
        return source.FollowUpEffect switch // 후속 효과의 적용 대상 선택
        { // 효과별 문구 시작
            CardFollowUpEffect.GainOwnerBlock => $" · 자신 방어도 +{value}", // 소유자 방어도 표시
            CardFollowUpEffect.HealAllAllies => $" · 생존 아군 전체 체력 {value} 회복", // 아군 전체 회복 표시
            CardFollowUpEffect.GainTargetBlock => $" · 선택한 아군 방어도 +{value}", // 선택 대상의 방어도 표시
            CardFollowUpEffect.DrawCards => $" · 카드 {value}장 드로우", // 추가 드로우 표시
            _ => string.Empty // 후속 효과 없는 원본의 기존 표시 유지
        }; // 후속 효과 문구 반환
    } // 후속 효과 표시 종료
    public void Hide() // 카드 상세 정보 숨김
    { // 툴팁 숨김 시작
        gameObject.SetActive(false); // 툴팁 오브젝트 숨김
    } // 툴팁 숨김 종료
    private void EnsureVisualStructure() // 툴팁 내부 UI 준비
    { // 화면 구조 준비 시작
        if (visualStructureCreated) // 기존 화면 구조 확인
        { // 기존 구조 처리 시작
            return; // 화면 구조 생성 중단
        } // 기존 구조 처리 종료
        Image backgroundImage = GetComponent<Image>(); // 툴팁 배경 이미지 조회
        if (backgroundImage == null) // 툴팁 배경 누락 확인
        { // 툴팁 배경 추가 시작
            backgroundImage = gameObject.AddComponent<Image>(); // 툴팁 배경 이미지 추가
        } // 툴팁 배경 추가 종료
        backgroundImage.color = new Color(0.035f, 0.045f, 0.07f, 0.98f); // 툴팁 배경 색상 설정
        LobbyUiArt.Apply(backgroundImage, "Button"); // 기존 제작 금속 이미지로 카드 상세 배경 적용
        backgroundImage.raycastTarget = false; // 툴팁 입력 차단 해제
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>(); // 툴팁 입력 제어 조회
        if (canvasGroup == null) // 입력 제어 누락 확인
        { // 입력 제어 추가 시작
            canvasGroup = gameObject.AddComponent<CanvasGroup>(); // 입력 제어 컴포넌트 추가
        } // 입력 제어 추가 종료
        canvasGroup.interactable = false; // 툴팁 상호작용 차단
        canvasGroup.blocksRaycasts = false; // 툴팁 포인터 차단 해제
        titleText = CreateText("TitleText", transform, 22f, Color.white, TextAlignmentOptions.Center); // 카드 이름 텍스트 생성
        SetRect(titleText.rectTransform, new Vector2(0f, 0.72f), Vector2.one, new Vector2(12f, 0f), new Vector2(-12f, -8f)); // 카드 이름 영역 설정
        detailText = CreateText("DetailText", transform, 15f, new Color(0.86f, 0.9f, 1f, 1f), TextAlignmentOptions.TopLeft); // 카드 상세 텍스트 생성
        detailText.overflowMode = TextOverflowModes.Ellipsis; // 넘친 상세 정보 생략 설정
        SetRect(detailText.rectTransform, Vector2.zero, new Vector2(1f, 0.72f), new Vector2(14f, 10f), new Vector2(-14f, -2f)); // 카드 상세 영역 설정
        visualStructureCreated = true; // 화면 구조 생성 완료 저장
    } // 화면 구조 준비 종료
    private static string GetTargetLabel(CardInstance card) // 카드 대상 문구 조회
    { // 대상 문구 조회 시작
        if (card.SourceData.ShieldRule == ShieldCardRule.SplitBarrier) // 분할 방벽의 순차 아군 선택 확인
        { // 분할 방벽 대상 문구 시작
            return "서로 다른 아군 2명 선택"; // 두 명의 대상 선택 안내
        } // 분할 방벽 대상 문구 종료
        switch (card.TargetType) // 카드 대상 종류 분기
        { // 대상 종류 분기 시작
            case CardTargetType.Self: // 자신 대상
                return "자신"; // 자신 문구 반환
            case CardTargetType.SingleAlly: // 단일 아군 대상
                return "아군 1명"; // 단일 아군 문구 반환
            case CardTargetType.FallenAlly: // 전투불능 아군 대상
                return "전투불능 아군 1명"; // 부활 대상 문구 반환
            case CardTargetType.EnemyAndAlly: // 적과 아군의 순차 지정
                return "적 1명 → 아군 1명"; // 서로 다른 두 대상의 선택 순서 표시
            case CardTargetType.AllAllies: // 전체 아군 대상
                return "모든 아군"; // 전체 아군 문구 반환
            case CardTargetType.SingleEnemy: // 단일 적 대상
                return "적 1명"; // 단일 적 문구 반환
            case CardTargetType.AllEnemies: // 전체 적 대상
                return "모든 적"; // 전체 적 문구 반환
            default: // 알 수 없는 대상
                return "알 수 없음"; // 기본 대상 문구 반환
        } // 대상 종류 분기 종료
    } // 대상 문구 조회 종료
    private static string GetEffectLabel(CardInstance cardInstance) // 카드 효과 문구 조회
    { // 효과 문구 조회 시작
        if (cardInstance.EffectType == CardEffectType.DrawThenDiscard) // 달의 드로우 후 손패 정리 확인
        { // 손패 정리 이름 시작
            return "드로우 후 손패 정리"; // 달 카드 효과 이름 반환
        } // 손패 정리 이름 종료
        if (cardInstance.EffectType == CardEffectType.TransferAllyDebuff) // 동료 디버프 이전 효과 확인
        { // 디버프 이전 이름 시작
            return "동료 디버프 이전"; // 디버프 이전 효과 이름 반환
        } // 디버프 이전 이름 종료
        if (cardInstance.EffectType == CardEffectType.RerollRandomAttack) // 무작위 공격 재굴림 확인
        { // 재굴림 이름 시작
            return "공격 대상 재굴림"; // 재굴림 효과 이름 반환
        } // 재굴림 이름 종료
        if (cardInstance.EffectType == CardEffectType.ShareAllyAttack) // 동료 피해 분담 확인
        { // 피해 분담 이름 시작
            return "동료 피해 분담"; // 피해 분담 효과 이름 반환
        } // 피해 분담 이름 종료
        if (cardInstance.EffectType == CardEffectType.GuardAllyAttack) // 동료 보호 효과 확인
        { // 동료 보호 이름 시작
            return "동료 보호"; // 동료 보호 효과 이름 반환
        } // 동료 보호 이름 종료
        if (cardInstance.EffectType == CardEffectType.ArrangeDrawTop) // 덱 순서 정리 효과 확인
        { // 덱 순서 정리 이름 시작
            return "덱 순서 정리"; // 덱 순서 정리 효과 이름 반환
        } // 덱 순서 정리 이름 종료
        if (cardInstance.EffectType == CardEffectType.GainBlock) // 방어도 획득 효과 확인
        { // 방어도 문구 시작
            if (cardInstance.SourceData.GrantsBlockToEachAlly) // 생존자마다 같은 방어도를 받는 카드 확인
            { // 개별 방어 문구 시작
                return "생존 아군마다 방어도 획득"; // 아군 한 명당 적용하는 수치 안내
            } // 개별 방어 문구 종료
            return cardInstance.TargetType == CardTargetType.AllAllies ? "전체 방어도 배분" : "방어도 획득"; // 대상별 방어도 문구 반환
        } // 방어도 문구 종료
        if (cardInstance.EffectType == CardEffectType.Heal) // 회복 효과 확인
        { // 회복 효과 처리 시작
            return "체력 회복"; // 회복 문구 반환
        } // 회복 효과 처리 종료
        if (cardInstance.EffectType == CardEffectType.ApplyStatusEffect) // 상태 이상 효과 확인
        { // 상태 이상 처리 시작
            return BattleStatusEffectInstance.GetDisplayName(cardInstance.StatusEffectType); // 상태 이상 이름 반환
        } // 상태 이상 처리 종료
        if (cardInstance.EffectType == CardEffectType.RemoveDebuffs || cardInstance.EffectType == CardEffectType.RemoveOneDebuff) // 디버프 해제 효과 확인
        { // 디버프 해제 처리 시작
            return "디버프 해제"; // 디버프 해제 문구 반환
        } // 디버프 해제 처리 종료
        if (cardInstance.EffectType == CardEffectType.ChangeMental) // 정신력 효과 확인
        { // 정신력 효과 처리 시작
            return "정신력 변화"; // 정신력 효과 문구 반환
        } // 정신력 효과 처리 종료
        if (cardInstance.DamageType == BattleDamageType.Magical) // 마법 피해 확인
        { // 마법 피해 처리 시작
            return "마법 피해"; // 마법 피해 문구 반환
        } // 마법 피해 처리 종료
        return "물리 피해"; // 기본 물리 피해 문구 반환
    } // 효과 문구 조회 종료
    private static string GetEffectDetail(CardInstance cardInstance) // 카드 효과 상세 문구 조회
    { // 효과 상세 조회 시작
        if (cardInstance.TargetType == CardTargetType.EnemyAndAlly) // 적 피해와 아군 회복을 나눈 복합 카드 확인
        { // 두 대상의 효과 표시 시작
            return $"적 {GetEffectLabel(cardInstance)} {cardInstance.EffectValue} · 아군 체력 {cardInstance.SourceData.SecondaryEffectValue} 회복"; // 카드별 현재 피해와 원본 회복량 안내
        } // 두 대상의 효과 표시 종료
        if (cardInstance.EffectType == CardEffectType.ReviveAlly) // 전투불능 아군의 부활 효과 확인
        { // 부활 상세 문구 처리 시작
            return $"최대 체력의 {cardInstance.SourceData.EffectValue}%로 부활 · 소수점 올림"; // 실제 부활 체력 계산 방식 표시
        } // 부활 상세 문구 처리 종료
        if (cardInstance.SourceData.ShieldRule == ShieldCardRule.GuardianHeart) // 치명적인 적 직접 타격을 기다리는 보호 확인
        { // 수호자의 심장 상세 시작
            return "다른 아군 1명의 치명적인 적 직접 타격 1회 대신 받기 · 시전자 체력 최소 1"; // 지정 대상과 발동 조건 및 생존 한도 표시
        } // 수호자의 심장 상세 종료
        string shieldDetail = ShieldCardCombat.GetEffectDetail(cardInstance); // 복합 방패 카드와 일회성 보호의 실제 효과 표시
        if (shieldDetail != null) // 방패 전용 설명이 있는 카드 확인
        { // 방패 카드 상세 반환 시작
            return shieldDetail; // 실제 카드 수치와 상태 수명에 맞춘 효과 문구
        } // 방패 카드 상세 반환 종료
        if (cardInstance.SourceData.ShieldRule == ShieldCardRule.RendingShield) // 현재 방어도를 기본 피해에 연결하는 카드 확인
        { // 방어도 피해 상세 표시 시작
            return $"기본 물리 피해 {cardInstance.OwnerUnit.CurrentBlock / 2} · 현재 방어도의 절반"; // 현재 방어도로 계산한 실제 기본 피해 표시
        } // 방어도 피해 상세 표시 종료
        if (cardInstance.SourceData.ShieldRule == ShieldCardRule.Taunt) // 생존 적의 단일 공격을 유도하는 방패 카드 확인
        { // 전체 도발 상세 표시 시작
            return $"다음 적 턴 단일 공격 유도 · 물리 방어력 +{cardInstance.EffectValue}"; // 유도 범위와 방어력 효과 표시
        } // 전체 도발 상세 표시 종료
        if (cardInstance.StatusEffectType == BattleStatusEffectType.DangerInterception) // 피격 한 번을 기다리는 상태의 카드 확인
        { // 위험 차단 상세 표시 시작
            return $"다음 직접 공격 피해 −{cardInstance.EffectValue}% · 1회"; // 턴이 아닌 공격 횟수 안내
        } // 위험 차단 상세 표시 종료
        if (cardInstance.SourceData.SwordRule == SwordCardRule.Finisher) // 남은 행동력 공격 확인
        { // 처형 피해 표시 시작
            return $"물리 피해 X × {cardInstance.EffectValue}"; // 남은 행동력과 실제 배수 표시
        } // 처형 피해 표시 종료
        if (cardInstance.SourceData.SwordRule == SwordCardRule.Counterattack) // 실제 체력 손실 반격 확인
        { // 역공 피해 표시 시작
            return $"현재 반격 기본 피해 {SwordCardCombat.BaseDamage(cardInstance, 0)}"; // 현재 누적 손실로 계산한 피해 표시
        } // 역공 피해 표시 종료
        if (cardInstance.EffectType == CardEffectType.Unplayable) // 손패에 남는 사용 불가 카드 확인
        { // 저주 상세 시작
            return "사용 불가"; // 실제 사용 제한 안내
        } // 저주 상세 종료
        if (cardInstance.EffectType == CardEffectType.Heal && cardInstance.SourceData.StatusRemovedByHealing != BattleStatusEffectType.None) // 지정 상태를 제거하는 회복 확인
        { // 복합 회복 상세 시작
            return $"{BattleStatusEffectInstance.GetDisplayName(cardInstance.SourceData.StatusRemovedByHealing)} 해제 · 체력 {cardInstance.EffectValue} 회복"; // 해제 상태와 실제 회복 수치 반환
        } // 복합 회복 상세 종료
        if (cardInstance.SourceData.CardId == "CRD_ARC_20_RESCUE") // 심판의 위기 아군 회복 확인
        { // 심판 회복 상세 시작
            return $"체력 {cardInstance.EffectValue} 회복 · 체력 50% 이하에서 {cardInstance.SourceData.SecondaryEffectValue} 추가 회복"; // 회복량과 위험 구간 상세 반환
        } // 심판 회복 상세 종료
        if (cardInstance.SourceData.CardId == "CRD_ARC_21_LINK") // 세계의 동료 연계 방어 확인
        { // 세계 연계 상세 시작
            return $"방어도 {cardInstance.EffectValue} · 다른 동료 둘의 전용 카드 사용 시 체력 비율이 가장 낮은 다른 아군에게 {cardInstance.SourceData.SecondaryEffectValue} 추가"; // 기본 방어와 연계 조건 상세 반환
        } // 세계 연계 상세 종료
        if (cardInstance.EffectType == CardEffectType.DrawThenDiscard) // 달의 드로우 후 손패 정리 확인
        { // 손패 정리 상세 시작
            return $"최대 {cardInstance.EffectValue}장 드로우 후 손패 하나 선택해 버리기"; // 달 카드의 드로우와 선택 상세 반환
        } // 손패 정리 상세 종료
        if (SunChainAttack.IsChainCard(cardInstance)) // 태양의 서로 다른 적 연속 공격 확인
        { // 태양 연속 공격 상세 시작
            return $"선택한 적 피해 {cardInstance.EffectValue} · 다른 무작위 적 피해 {cardInstance.SourceData.SecondaryEffectValue}"; // 태양 카드의 두 공격 수치 반환
        } // 태양 연속 공격 상세 종료
        if (cardInstance.SourceData.CardId == "CRD_ARC_16_SHATTER") // 탑의 광역 방어도 파괴 카드 확인
        { // 광역 방어도 파괴 상세 시작
            return $"모든 적의 방어도 {cardInstance.SourceData.SecondaryEffectValue} 제거 후 물리 피해 {cardInstance.EffectValue}"; // 제거 순서와 피해 상세 반환
        } // 광역 방어도 파괴 상세 종료
        if (DevilBloodPrice.IsBloodPriceCard(cardInstance)) // 악마 체력 소모 전용 카드 확인
        { // 체력 비용 상세 시작
            return $"체력 {cardInstance.SourceData.SecondaryEffectValue} 소모 · 물리 피해 {cardInstance.EffectValue} · 소모 후 생존 필요"; // 체력 비용과 피해 상세 반환
        } // 체력 비용 상세 종료
        if (cardInstance.SourceData.CardId == "CRD_ARC_13_FINISH") // 죽음의 마무리 전용 카드 확인
        { // 마무리 공격 상세 시작
            return $"물리 피해 {cardInstance.EffectValue} · 디버프 대상 피해 50% 증가"; // 조건부 피해 상세 반환
        } // 마무리 공격 상세 종료
        if (cardInstance.EffectType == CardEffectType.TransferAllyDebuff) // 동료 디버프 이전 효과 확인
        { // 디버프 이전 상세 시작
            return "다른 아군의 디버프 하나를 선택해 자신에게 이전"; // 디버프 이전 상세 반환
        } // 디버프 이전 상세 종료
        if (cardInstance.EffectType == CardEffectType.RerollRandomAttack) // 무작위 공격 재굴림 확인
        { // 재굴림 상세 시작
            return "무작위 공격 대상 재선택. 같은 대상도 선택 가능"; // 재굴림 상세 반환
        } // 재굴림 상세 종료
        if (cardInstance.EffectType == CardEffectType.ShareAllyAttack) // 동료 피해 분담 확인
        { // 피해 분담 상세 시작
            return "다른 아군의 첫 예정 공격을 방어 후 절반씩 분담"; // 피해 분담 상세 반환
        } // 피해 분담 상세 종료
        if (cardInstance.EffectType == CardEffectType.GuardAllyAttack) // 동료 보호 효과 확인
        { // 동료 보호 상세 시작
            return "다른 아군의 첫 예정 공격 대신 받기"; // 동료 보호 상세 반환
        } // 동료 보호 상세 종료
        if (cardInstance.EffectType == CardEffectType.ArrangeDrawTop) // 덱 순서 정리 효과 확인
        { // 덱 순서 정리 상세 시작
            return $"덱 위 최대 {cardInstance.EffectValue}장 확인 후 한 장을 맨 위로 이동"; // 덱 순서 정리 상세 반환
        } // 덱 순서 정리 상세 종료
        if (cardInstance.EffectType == CardEffectType.SelectPartner) // 파트너 지정 효과 확인
        { // 파트너 상세 처리 시작
            return "생존한 다른 아군을 파트너로 지정"; // 파트너 지정 상세 반환
        } // 파트너 상세 처리 종료
        switch (cardInstance.SourceData.CupRule) // 성배 지원 카드의 추가 효과 확인
        { // 성배 지원 설명 분기 시작
            case CupCardRule.Calming: // 공포 해제와 정신력 회복 안내
                return $"공포 해제 · 정신력 +{cardInstance.MentalChangeValue}"; // 진정의 두 독립 효과 표시
            case CupCardRule.HolyWater: // 성수의 조건부 언데드 피해 안내
                return $"마법 피해 {cardInstance.EffectValue} · 언데드에게 추가 +{cardInstance.SourceData.SecondaryEffectValue}"; // 일반과 언데드 대상의 피해 구분 표시
            case CupCardRule.PurifyingTouch: // 약화 제거와 마법 저항 안내
                return $"디버프 1개 해제 · 직접 마법 피해 {cardInstance.SourceData.SecondaryEffectValue}% 감소 ({cardInstance.StatusDuration} 적 턴)"; // 정화의 손길의 의미와 실제 지속 시간 표시
        } // 성배 지원 설명 분기 종료
        if (cardInstance.EffectType == CardEffectType.RemoveDebuffs || cardInstance.EffectType == CardEffectType.RemoveOneDebuff) // 디버프 해제 효과 확인
        { // 디버프 해제 상세 처리 시작
            return cardInstance.EffectType == CardEffectType.RemoveOneDebuff ? "먼저 적용된 디버프 1개 해제" : "모든 디버프 해제"; // 카드별 디버프 해제 상세 반환
        } // 디버프 해제 상세 처리 종료
        if (cardInstance.EffectType == CardEffectType.ApplyStatusEffect) // 상태 이상 효과 확인
        { // 상태 이상 상세 처리 시작
            string effectName = GetEffectLabel(cardInstance); // 상태 이상 이름 조회
            string valueLabel = cardInstance.StatusEffectType == BattleStatusEffectType.StatusImmunity ? string.Empty : $" {cardInstance.EffectValue}"; // 면역 외 효과 수치 생성
            return $"{effectName}{valueLabel}  |  지속 {cardInstance.StatusDuration}T  |  최대 {cardInstance.StatusMaximumStacks}중첩"; // 상태 이상 상세 반환
        } // 상태 이상 상세 처리 종료
        if (cardInstance.EffectType == CardEffectType.ChangeMental) // 정신력 효과 확인
        { // 정신력 상세 처리 시작
            string sign = cardInstance.MentalChangeValue > 0 ? "+" : string.Empty; // 양수 부호 문구 계산
            return $"정신력 {sign}{cardInstance.MentalChangeValue}"; // 정신력 상세 반환
        } // 정신력 상세 처리 종료
        string value = cardInstance.EffectValue.ToString(); // 기본 효과 수치 문구 생성
        if (cardInstance.SourceData.HasRandomDamage) // 두 기본 피해가 설정된 카드 확인
        { // 무작위 피해 문구 시작
            int high = CardUpgradeProfileCatalog.CalculateEffectValue(cardInstance.SourceData, cardInstance.SourceData.RandomDamageHighValue, cardInstance.UpgradeLevel); // 높은 피해의 실제 강화 수치
            value = $"{value} 또는 {high}"; // 두 가지 초기 피해를 같은 효과 줄에 표시
        } // 무작위 피해 문구 종료
        if (cardInstance.SourceData.DamageHitCount > 1) // 동일 대상 연속 공격 확인
        { // 연타 상세 시작
            return $"{GetEffectLabel(cardInstance)} {value} × {cardInstance.SourceData.DamageHitCount}회"; // 타격당 피해와 반복 횟수 반환
        } // 연타 상세 종료
        return $"{GetEffectLabel(cardInstance)} {value}"; // 피해와 회복 상세 반환
    } // 효과 상세 조회 종료
    private static TMP_Text CreateText(string objectName, Transform parent, float fontSize, Color textColor, TextAlignmentOptions alignment) // 공용 텍스트 생성
    { // 텍스트 생성 시작
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); // 텍스트 오브젝트 생성
        textObject.transform.SetParent(parent, false); // 텍스트 부모 연결
        TMP_Text text = textObject.GetComponent<TMP_Text>(); // 텍스트 컴포넌트 조회
        text.fontSize = fontSize; // 글자 크기 설정
        text.color = textColor; // 글자 색상 설정
        text.alignment = alignment; // 텍스트 정렬 설정
        text.raycastTarget = false; // 텍스트 입력 차단 해제
        text.font = ProjectCFontProvider.KoreanFontAsset; // 한글 지원 글꼴 적용
        return text; // 생성 텍스트 반환
    } // 텍스트 생성 종료
    private static void SetRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax) // 사각 영역 설정
    { // 사각 영역 설정 시작
        rectTransform.anchorMin = anchorMin; // 최소 앵커 설정
        rectTransform.anchorMax = anchorMax; // 최대 앵커 설정
        rectTransform.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 설정
        rectTransform.offsetMin = offsetMin; // 왼쪽 아래 여백 설정
        rectTransform.offsetMax = offsetMax; // 오른쪽 위 여백 설정
    } // 사각 영역 설정 종료
} // 클래스 종료
