using System.Collections.Generic; // 생성 상태 이미지 재사용 목록
using UnityEngine; // 상태 이미지와 스프라이트 사용
public static class BattleStatusIconArt // 상태 종류별 생성 아이콘 제공
{ // 상태 아이콘 제공 시작
    private static readonly Dictionary<int, Sprite> sprites = new Dictionary<int, Sprite>(); // 재사용할 상태 아이콘 목록
    public static Sprite Get(BattleStatusEffectType type) // 상태 종류별 정사각형 이미지 조회
    { // 상태 이미지 조회 시작
        if (type == BattleStatusEffectType.Vulnerable || type == BattleStatusEffectType.DamageNullification || type == BattleStatusEffectType.DistributedDefense || type == BattleStatusEffectType.RetributionBulwark || type == BattleStatusEffectType.FortifyResistance || type == BattleStatusEffectType.EternalShield || type == BattleStatusEffectType.EternalShieldResistance || type == BattleStatusEffectType.ReflectionCitadel || type == BattleStatusEffectType.GuardianHeart || type == BattleStatusEffectType.DefeatWard || type == BattleStatusEffectType.HealingReceivedUp || type == BattleStatusEffectType.Renewal || type == BattleStatusEffectType.SanctuaryWard || type == BattleStatusEffectType.FearResistance || type == BattleStatusEffectType.DelayedHealing || type == BattleStatusEffectType.Stealth || type == BattleStatusEffectType.ResurrectionSeed || type == BattleStatusEffectType.PainDistribution || type == BattleStatusEffectType.MiracleEcho || type == BattleStatusEffectType.CorrosionResistanceDown || type == BattleStatusEffectType.PulseCordAttackPowerUp || type == BattleStatusEffectType.CrystalMarkDefenseDown || type == BattleStatusEffectType.StethoscopeMark || type == BattleStatusEffectType.ArcaneWard || type == BattleStatusEffectType.MagicAttackPowerUp || type == BattleStatusEffectType.Shock || type == BattleStatusEffectType.StealthBreakDamageUp || type == BattleStatusEffectType.ManaConcentration || type == BattleStatusEffectType.IncomingMagicDamageUp || type == BattleStatusEffectType.EvasionDown || type == BattleStatusEffectType.ConfessionDamageMark || type == BattleStatusEffectType.WandEcho || type == BattleStatusEffectType.DanceShoeDamageUp || type == BattleStatusEffectType.WandOverload || type == BattleStatusEffectType.IllusionHitReduction || type == BattleStatusEffectType.ServantContractMark || type == BattleStatusEffectType.LordFinalOrderAttack || type == BattleStatusEffectType.LordFinalOrderWeakenedDefense ||
            type is BattleStatusEffectType.DistributionAttackUp or BattleStatusEffectType.DistributionPhysicalDefenseDown or BattleStatusEffectType.ToolMaintenanceAttackUp or BattleStatusEffectType.SandQueenSummonAttackGrow or BattleStatusEffectType.BattlefieldMapAccuracyUp or BattleStatusEffectType.GoldenLureAttackPercentDown) // 취약 상태의 개별 원화 확인
        { // 취약 아이콘 조회 시작
            return GetIndividualIcon(type); // VulnerableStatusIcon 원화 반환
        } // 취약 아이콘 조회 종료
        if (type == BattleStatusEffectType.Awakening || type == BattleStatusEffectType.Collapse || type == BattleStatusEffectType.Evasion || type == BattleStatusEffectType.Disorientation || type == BattleStatusEffectType.CriticalChanceUp || type == BattleStatusEffectType.ChargeDefenseDown || type == BattleStatusEffectType.Bleed || type == BattleStatusEffectType.Stagger || type == BattleStatusEffectType.Stun || type == BattleStatusEffectType.NextAttackDown || type == BattleStatusEffectType.AttackPowerDown || type == BattleStatusEffectType.BladeMaintenance || type == BattleStatusEffectType.PhysicalRiposte || type == BattleStatusEffectType.BloodOath || type == BattleStatusEffectType.Fear || type == BattleStatusEffectType.FinishingStance || type == BattleStatusEffectType.HealingReceivedDown || type == BattleStatusEffectType.BattlefieldResolve || type == BattleStatusEffectType.BladeFrenzy || type == BattleStatusEffectType.PublicMark || type == BattleStatusEffectType.ActionSpeedDown || type == BattleStatusEffectType.DuelMark || type == BattleStatusEffectType.SwordSaintStance || type == BattleStatusEffectType.ShockAbsorption || type == BattleStatusEffectType.ResistanceStance || type == BattleStatusEffectType.ActionSpeedUp || type == BattleStatusEffectType.ShieldBalance || type == BattleStatusEffectType.ShieldCounter || type == BattleStatusEffectType.GuardianCircle || type == BattleStatusEffectType.DangerInterception || type == BattleStatusEffectType.FocusedDefense || type == BattleStatusEffectType.BleedResistance || type == BattleStatusEffectType.UndyingGuard) // 별도 투명 아이콘 확인
        { // 정신 상태 아이콘 조회 시작
            return GetIndividualIcon(type); // 별도 제작한 상태 아이콘 반환
        } // 정신 상태 아이콘 조회 종료
        int index = GetIndex(type); // 상태 종류에 맞는 이미지 순서 조회
        if (sprites.TryGetValue(index, out Sprite cached) && cached != null) // 기존 상태 이미지 존재 확인
        { // 기존 이미지 반환 시작
            return cached; // 재사용 상태 이미지 반환
        } // 기존 이미지 반환 종료
        Texture2D texture = Resources.Load<Texture2D>("UI/Battle/StatusIcons"); // 제작한 상태 아이콘 이미지 조회
        if (texture == null || texture.width != texture.height || texture.width < 4) // 아이콘 이미지 유효성 확인
        { // 이미지 누락 처리 시작
            return null; // 상태 아이콘 없음 반환
        } // 이미지 누락 처리 종료
        int tileSize = texture.width / 4; // 네 칸 기준 정사각형 이미지 크기 계산
        int column = index % 4; // 상태 아이콘 가로 위치 계산
        int row = index / 4; // 상태 아이콘 위쪽 기준 세로 위치 계산
        Rect area = new Rect(column * tileSize, texture.height - (row + 1) * tileSize, tileSize, tileSize); // 실제 상태 아이콘 영역 계산
        Sprite sprite = Sprite.Create(texture, area, new Vector2(0.5f, 0.5f), 100f); // 개별 정사각형 상태 스프라이트 생성
        sprite.hideFlags = HideFlags.HideAndDontSave; // 런타임 공용 이미지 저장 제외
        sprites[index] = sprite; // 상태 아이콘 재사용 목록 보관
        return sprite; // 생성한 상태 아이콘 반환
    } // 상태 이미지 조회 종료
    private static Sprite GetIndividualIcon(BattleStatusEffectType type) // 개별 투명 상태 아이콘 조회
    { // 투명 아이콘 조회 시작
        int index = 16 + (int)type; // 공용 시트와 겹치지 않는 개별 상태 키
        if (sprites.TryGetValue(index, out Sprite cached) && cached != null) // 기존 아이콘 확인
        { // 기존 아이콘 반환 시작
            return cached; // 재사용 아이콘 반환
        } // 기존 아이콘 반환 종료
        string name = type + "StatusIcon"; // 상태 종류에 해당하는 개별 원화 이름
        Texture2D texture = Resources.Load<Texture2D>("UI/Battle/" + name); // 투명 상태 이미지 조회
        if (texture == null) // 상태 이미지 누락 확인
        { // 이미지 누락 처리 시작
            return null; // 이미지 없음 반환
        } // 이미지 누락 처리 종료
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f)); // 투명 상태 아이콘 생성
        sprite.hideFlags = HideFlags.HideAndDontSave; // 런타임 아이콘 저장 제외
        sprites[index] = sprite; // 생성한 아이콘 재사용 등록
        return sprite; // 정신 상태 아이콘 반환
    } // 투명 아이콘 조회 종료
    private static int GetIndex(BattleStatusEffectType type) // 상태 종류를 제작 이미지 순서로 변환
    { // 상태 이미지 순서 변환 시작
        switch (type) // 상태 종류 분기
        { // 상태 종류 분기 시작
            case BattleStatusEffectType.Poison: return 0; // 중독 아이콘 위치
            case BattleStatusEffectType.Burn: return 1; // 화상 아이콘 위치
            case BattleStatusEffectType.Blind: return 2; // 실명 아이콘 위치
            case BattleStatusEffectType.PhysicalDefenseDown: return 3; // 물리 방어 약화 아이콘 위치
            case BattleStatusEffectType.MagicalResistanceDown: return 4; // 마법 저항 약화 아이콘 위치
            case BattleStatusEffectType.Disrupt: return 5; // 교란 아이콘 위치
            case BattleStatusEffectType.ChariotMark: return 6; // 전차 표식 아이콘 위치
            case BattleStatusEffectType.Regeneration: return 7; // 재생 아이콘 위치
            case BattleStatusEffectType.AttackPowerUp: return 8; // 공격 증가 아이콘 위치
            case BattleStatusEffectType.StatusImmunity: return 9; // 상태 면역 아이콘 위치
            case BattleStatusEffectType.PhysicalDefenseUp: return 10; // 물리 방어 증가 아이콘 위치
            case BattleStatusEffectType.MagicalResistanceUp: return 11; // 마법 저항 증가 아이콘 위치
            case BattleStatusEffectType.StarReservedHealing: return 12; // 예약 회복 아이콘 위치
            case BattleStatusEffectType.StarPassiveReservedHealing: return 13; // 추가 예약 회복 아이콘 위치
            case BattleStatusEffectType.GuardPreparation: return 14; // 방어 준비 아이콘 위치
            default: return 15; // 알 수 없는 상태 기본 아이콘 위치
        } // 상태 종류 분기 종료
    } // 상태 이미지 순서 변환 종료
} // 상태 아이콘 제공 종료
