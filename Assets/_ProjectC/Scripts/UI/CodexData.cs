using System; // 상태 효과 열거형 사용
using System.Collections.Generic; // 도감 항목 목록 사용
using UnityEngine; // 실제 게임 리소스 사용

public sealed class CodexEntry // 도감에 표시할 실제 항목
{ // 도감 항목 시작
    public readonly string Name; // 항목 이름
    public readonly string Description; // 항목 설명
    public readonly Sprite Image; // 항목 이미지
    public CodexEntry(string name, string description, Sprite image) // 실제 원본 정보 보관
    { // 도감 항목 생성 시작
        Name = name; // 실제 이름 보관
        Description = description; // 실제 설명 보관
        Image = image; // 실제 이미지 보관
    } // 도감 항목 생성 종료
} // 도감 항목 종료
public static class CodexData // 실제 게임 데이터 기반 도감
{ // 도감 데이터 시작
    public static List<CodexEntry> Load(int category) // 선택 분류의 실제 항목 조회
    { // 도감 항목 조회 시작
        List<CodexEntry> entries = new List<CodexEntry>(); // 도감 항목 목록 생성
        SaveAssetCatalog catalog = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 실제 아이템 원본 목록 조회
        if (category == 0) // 상호작용 이벤트 분류 확인
        { // 이벤트 항목 수집 시작
            foreach (ExplorationEventData item in ExplorationEventCatalog.LoadEvents()) // 실제 탐사 이벤트 순회
            { // 이벤트 항목 추가 시작
                entries.Add(new CodexEntry(item.DisplayName, item.Description, item.IllustrationSprite)); // 이벤트 정보 등록
            } // 이벤트 항목 추가 종료
        } // 이벤트 항목 수집 종료
        else if (category == 1 && catalog != null) // 포션 원본 목록 확인
        { // 포션 항목 수집 시작
            foreach (ConsumableItemData item in catalog.Consumables) // 실제 소모품 목록 순회
            { // 소모품 분류 확인 시작
                if (item is PotionData) // 포션 자료형 확인
                { // 포션 항목 추가 시작
                    entries.Add(new CodexEntry(item.DisplayName, item.Description, item.Icon)); // 실제 포션 정보 등록
                } // 포션 항목 추가 종료
            } // 소모품 분류 확인 종료
        } // 포션 항목 수집 종료
        else if (category == 2) // 적 분류 확인
        { // 적 항목 수집 시작
            HashSet<string> seen = new HashSet<string>(); // 중복 적 식별자 보관
            foreach (EncounterData encounter in Resources.LoadAll<EncounterData>("Encounters")) // 실제 탐사 조우 목록 순회
            { // 조우별 적 수집 시작
                foreach (EnemyData enemy in encounter.Enemies) // 조우의 적 목록 순회
                { // 적 원본 확인 시작
                    if (enemy != null && seen.Add(enemy.EnemyId)) // 중복되지 않은 적 확인
                    { // 적 항목 추가 시작
                        entries.Add(new CodexEntry(enemy.DisplayName, enemy.Description, enemy.Portrait)); // 실제 적 정보 등록
                    } // 적 항목 추가 종료
                } // 적 원본 확인 종료
            } // 조우별 적 수집 종료
        } // 적 항목 수집 종료
        else if (category == 3 && catalog != null) // 유물 원본 목록 확인
        { // 유물 항목 수집 시작
            foreach (RelicData item in catalog.Relics) // 실제 유물 목록 순회
            { // 유물 원본 확인 시작
                if (item != null) // 유물 존재 여부 확인
                { // 유물 항목 추가 시작
                    entries.Add(new CodexEntry(item.DisplayName, item.Description, item.Icon)); // 실제 유물 정보 등록
                } // 유물 항목 추가 종료
            } // 유물 원본 확인 종료
        } // 유물 항목 수집 종료
        else if (category == 4) // 상태 효과 분류 확인
        { // 상태 효과 수집 시작
            foreach (BattleStatusEffectType effect in Enum.GetValues(typeof(BattleStatusEffectType))) // 게임 상태 효과 순회
            { // 상태 효과 확인 시작
                if (effect != BattleStatusEffectType.None) // 빈 상태 제외
                { // 상태 효과 추가 시작
                    string description = string.Empty; // 수치가 정해진 상태만 표시할 설명
                    if (effect == BattleStatusEffectType.CriticalChanceUp) // 전의 고취 강화 확인
                    { // 치명 강화 설명 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, 5) + ". 2턴 지속. 다시 받으면 지속 시간 갱신."; // 기본 확률 증가와 중복 적용 규칙
                    } // 치명 강화 설명 종료
                    else if (effect == BattleStatusEffectType.ChargeDefenseDown) // 돌진 약화 확인
                    { // 돌진 약화 설명 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, 2) + ". 돌진에 명중한 적에게 다음 아군 턴 동안 적용."; // 피격 대상과 지연 발동 조건
                    } // 돌진 약화 설명 종료
                    else if (effect == BattleStatusEffectType.Bleed || effect == BattleStatusEffectType.NextAttackDown) // 피해 수치를 표시할 검 카드 디버프
                    { // 상태 수치 설명 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, 2); // 턴 종료 출혈 또는 다음 공격 약화 설명
                    } // 상태 수치 설명 종료
                    else if (effect == BattleStatusEffectType.Stagger || effect == BattleStatusEffectType.Stun || effect == BattleStatusEffectType.Fear) // 적 행동이나 회피에 제약을 주는 상태
                    { // 행동 제한 설명 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, 1); // 다음 행동 취소 또는 기절 설명
                    } // 행동 제한 설명 종료
                    else if (effect == BattleStatusEffectType.BloodOath || effect == BattleStatusEffectType.FinishingStance || effect == BattleStatusEffectType.HealingReceivedDown) // 원본 수치가 있는 맹세와 자세 및 약화 확인
                    { // 카드 상태 설명 시작
                        int value = effect == BattleStatusEffectType.BloodOath ? 5 : effect == BattleStatusEffectType.FinishingStance ? 30 : 50; // 시트의 검 피해와 마무리 및 회복 변화 수치
                        description = BattleStatusEffectInstance.GetDescription(effect, value); // 도감의 상태 수치와 전투 안내 일치
                    } // 카드 상태 설명 종료
                    else if (effect == BattleStatusEffectType.BladeMaintenance || effect == BattleStatusEffectType.PhysicalRiposte) // 검 정비와 반사 상태의 원본 수치 확인
                    { // 검 상태 설명 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, effect == BattleStatusEffectType.BladeMaintenance ? 3 : 50); // 첫 검 공격과 물리 반사의 상태 설명
                    } // 검 상태 설명 종료
                    else if (effect == BattleStatusEffectType.AttackPowerDown) // 청동 경보장치의 한 턴 약화
                    { // 경보 설명 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, 1) + ". 적 턴이 끝나면 해제."; // 적용 공격과 종료 시점 표시
                    } // 경보 설명 종료
                    Sprite icon = description.Length > 0 ? BattleStatusIconArt.Get(effect) : null; // 검 카드의 독립 상태 아이콘 조회
                    entries.Add(new CodexEntry(BattleStatusEffectInstance.GetDisplayName(effect), description, icon)); // 실제 상태 이름과 연결된 상세 정보 등록
                } // 상태 효과 추가 종료
            } // 상태 효과 확인 종료
        } // 상태 효과 수집 종료
        return entries; // 실제 원본 항목 반환
    } // 도감 항목 조회 종료
} // 도감 데이터 종료
