using System; // 상태 효과 열거형 사용
using System.Collections.Generic; // 도감 항목 목록 사용
using UnityEngine; // 실제 게임 리소스 사용

public sealed class CodexEntry // 도감에 표시할 실제 항목
{ // 도감 항목 시작
    public readonly string Name; // 항목 이름
    public readonly string Description; // 항목 설명
    private readonly string artworkKey; // 독립 썸네일과 원화 참조의 리소스 키
    private readonly Func<Sprite> statusIcon; // 상태 분류의 늦은 아이콘 조회
    public bool HasArtwork => !string.IsNullOrEmpty(artworkKey) || statusIcon != null; // 항목에 등록된 이미지 여부
    public Sprite Image => statusIcon != null ? statusIcon() : string.IsNullOrEmpty(artworkKey) ? null : Resources.Load<CodexArtworkResource>("Codex/Originals/" + artworkKey)?.Artwork; // 기존 호출자가 명시적으로 조회한 원본 이미지
    internal CodexEntry(string name, string description, string artworkKey, Func<Sprite> statusIcon = null) // 이미지 없이 도감 문자와 키 보관
    { // 도감 항목 생성 시작
        Name = name; // 실제 이름 보관
        Description = description; // 실제 설명 보관
        this.artworkKey = artworkKey; // 실제 이미지 조회에 사용할 키 보관
        this.statusIcon = statusIcon; // 상태 이미지 필요 시 사용할 조회 함수
    } // 도감 항목 생성 종료
    public CodexEntry WithName(string name) // 같은 이미지 계약으로 표시 이름만 바꾼 항목 생성
    { // 이름 복사 시작
        return new CodexEntry(name, Description, artworkKey, statusIcon); // 도감 이미지 없이 새 이름 사용
    } // 이름 복사 종료
    public Sprite LoadThumbnail() // 현재 표시되는 셀의 작은 이미지 조회
    { // 썸네일 조회 시작
        return !string.IsNullOrEmpty(artworkKey) ? Resources.Load<Sprite>("Codex/Thumbnails/CodexThumb_" + artworkKey) : null; // 상세 원화와 분리된 작은 이미지 조회
    } // 썸네일 조회 종료
    internal ResourceRequest RequestFullArtwork() // 상세를 선택한 경우 원화 리소스 조회
    { // 원화 요청 시작
        return statusIcon == null && !string.IsNullOrEmpty(artworkKey) ? Resources.LoadAsync<CodexArtworkResource>("Codex/Originals/" + artworkKey) : null; // 한 원화만 참조하는 작은 리소스 비동기 로드
    } // 원화 요청 종료
    internal Sprite LoadStatusArtwork() // 선택 상세에 작은 상태 원본 표시
    { // 상태 원화 조회 시작
        return statusIcon?.Invoke(); // 독립 원본이 없을 때 실제 상태 아이콘 조회
    } // 상태 원화 조회 종료
} // 도감 항목 종료
public static class CodexData // 실제 게임 데이터 기반 도감
{ // 도감 데이터 시작
    [Serializable] // 텍스트로 내보낸 도감 행 구조
    private sealed class Row // 유니티 에셋 참조 없는 도감 자료
    { // 도감 행 시작
        public int category; // 도감 분류 번호
        public string name; // 실제 항목 이름
        public string description; // 실제 항목 설명
        public string artworkKey; // 원화와 썸네일의 조회 키
    } // 도감 행 종료
    [Serializable] // 도감 텍스트 목록 구조
    private sealed class Index // 도감 텍스트 전체 목록
    { // 목록 구조 시작
        public Row[] rows; // 원본 순서를 유지한 항목 행
    } // 목록 구조 종료
    private static Row[] rows; // 첫 조회 후 재사용할 문자와 이미지 키
    public static List<CodexEntry> Load(int category) // 선택 분류의 실제 항목 조회
    { // 도감 항목 조회 시작
        List<CodexEntry> entries = new List<CodexEntry>(); // 도감 항목 목록 생성
        if ((category >= 0 && category < 4) || category == 7) // 탐사 사건과 포션 및 적과 유물과 카드 분류 확인
        { // 문자열 목록 수집 시작
            foreach (Row row in LoadRows()) // 원화 의존성 없는 도감 원본 순회
            { // 해당 목록의 항목 확인 시작
                if (row.category == category) // 선택 분류에 포함된 실제 데이터 확인
                { // 도감 항목 추가 시작
                    entries.Add(new CodexEntry(row.name, row.description, row.artworkKey)); // 필요한 화면에서만 읽을 이미지 키 기록
                } // 도감 항목 추가 종료
            } // 해당 목록의 항목 확인 종료
        } // 문자열 목록 수집 종료
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
                    else if (effect == BattleStatusEffectType.BattlefieldResolve || effect == BattleStatusEffectType.BladeFrenzy) // 다음 턴 방어와 이번 턴 검 피해 강화
                    { // 특수 검 상태 설명 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, 4); // 원본 시트의 방어도와 추가 피해 수치
                    } // 특수 검 상태 설명 종료
                    else if (effect == BattleStatusEffectType.BladeMaintenance || effect == BattleStatusEffectType.PhysicalRiposte) // 검 정비와 반사 상태의 원본 수치 확인
                    { // 검 상태 설명 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, effect == BattleStatusEffectType.BladeMaintenance ? 3 : 50); // 첫 검 공격과 물리 반사의 상태 설명
                    } // 검 상태 설명 종료
                    else if (effect == BattleStatusEffectType.AttackPowerDown) // 청동 경보장치의 한 턴 약화
                    { // 경보 설명 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, 1) + ". 적 턴 종료마다 남은 기간이 줄어든다."; // 새 방패 카드와 기존 공격력 약화의 기간 표시
                    } // 경보 설명 종료
                    else if (effect == BattleStatusEffectType.DuelMark || effect == BattleStatusEffectType.SwordSaintStance) // 전설 검 카드의 표식과 자세 확인
                    { // 전설 검 상태 설명 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, effect == BattleStatusEffectType.DuelMark ? 40 : 4); // 원본의 피해 비율과 타격 강화 수치
                    } // 전설 검 상태 설명 종료
                    else if (effect == BattleStatusEffectType.ShockAbsorption || effect == BattleStatusEffectType.ResistanceStance) // 방패 카드의 두 직접 피해 경감 확인
                    { // 방패 상태 설명 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, effect == BattleStatusEffectType.ShockAbsorption ? 5 : 15); // 원본의 고정 피해와 마법 비율 경감 수치
                    } // 방패 상태 설명 종료
                    else if (effect == BattleStatusEffectType.ActionSpeedUp) // 전투 시작에 보이는 속도 상승 상태
                    { // 속도 상태 설명 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, 1); // 바람개비 배지의 속도 보정 수치 연결
                    } // 속도 상태 설명 종료
                    else if (effect == BattleStatusEffectType.ShieldBalance || effect == BattleStatusEffectType.ShieldCounter || effect == BattleStatusEffectType.GuardianCircle || effect == BattleStatusEffectType.DangerInterception) // 방패 지원 카드의 네 상태 확인
                    { // 신규 방패 상태의 도감 설명 시작
                        int amount = effect switch // 원본 방패 카드의 상태 수치 결정
                        { // 방패 상태 수치 분기 시작
                            BattleStatusEffectType.ShieldBalance => 1, // 다음 방패의 행동력 할인
                            BattleStatusEffectType.ShieldCounter => 8, // 방어 성공 반격의 고정 피해
                            BattleStatusEffectType.GuardianCircle => 10, // 직접 피해 비율 경감
                            _ => 70 // 위험 차단의 한 번의 피해 경감
                        }; // 원본 카드 수치 지정 종료
                        description = BattleStatusEffectInstance.GetDescription(effect, amount); // 새 방패 상태의 실제 게임 수치 안내
                    } // 신규 방패 상태의 도감 설명 종료
                    else if (effect == BattleStatusEffectType.FocusedDefense || effect == BattleStatusEffectType.BleedResistance || effect == BattleStatusEffectType.UndyingGuard) // 집중 방어와 출혈 저항 및 불굴 확인
                    { // 새 방패 상태 상세 안내 시작
                        int amount = effect == BattleStatusEffectType.FocusedDefense ? 50 : effect == BattleStatusEffectType.BleedResistance ? 20 : 1; // 기본 방패 카드의 원본 수치
                        description = BattleStatusEffectInstance.GetDescription(effect, amount); // 새 방패 상태 도감 설명 연결
                    } // 새 방패 상태 상세 안내 종료
                    else if (effect == BattleStatusEffectType.DamageNullification) // 피해 무효 상태 확인
                    { // 피해 무효 도감 설명 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, 1); // 피해 무효의 실제 카드 기본 수치와 설명
                    } // 피해 무효 도감 설명 종료
                    else if (effect == BattleStatusEffectType.DistributedDefense) // 분산 방어 상태 확인
                    { // 분산 방어 도감 설명 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, 35); // 분산 방어의 실제 카드 기본 수치와 설명
                    } // 분산 방어 도감 설명 종료
                    else if (effect == BattleStatusEffectType.RetributionBulwark) // 응징 방벽 상태 확인
                    { // 응징 방벽 도감 설명 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, 5); // 응징 방벽의 실제 카드 기본 수치와 설명
                    } // 응징 방벽 도감 설명 종료
                    else if (effect == BattleStatusEffectType.FortifyResistance) // 요새화 저항 상태 확인
                    { // 요새화 저항 도감 설명 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, 10); // 요새화 저항의 실제 카드 기본 수치와 설명
                    } // 요새화 저항 도감 설명 종료
                    else if (effect == BattleStatusEffectType.EternalShield) // 영원의 방패 상태 확인
                    { // 영원의 방패 도감 설명 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, 30); // 영원의 방패의 실제 카드 기본 수치와 설명
                    } // 영원의 방패 도감 설명 종료
                    else if (effect == BattleStatusEffectType.EternalShieldResistance) // 영원의 방패 저항 상태 확인
                    { // 영원의 방패 저항 도감 설명 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, 20); // 영원의 방패 저항의 실제 카드 기본 수치와 설명
                    } // 영원의 방패 저항 도감 설명 종료
                    else if (effect == BattleStatusEffectType.ReflectionCitadel) // 반사의 성채 상태 확인
                    { // 반사의 성채 도감 설명 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, 50); // 반사의 성채의 실제 카드 기본 수치와 설명
                    } // 반사의 성채 도감 설명 종료
                    else if (effect == BattleStatusEffectType.GuardianHeart) // 수호자의 심장 상태 확인
                    { // 수호자의 심장 도감 설명 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, 1); // 수호자의 심장의 실제 카드 기본 수치와 설명
                    } // 수호자의 심장 도감 설명 종료
                    else if (effect == BattleStatusEffectType.DefeatWard) // 성배의 전투불능 방지 상태 확인
                    { // 전투불능 방지 상세 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, 1); // 한 번 치명 피해를 막는 실제 카드 설명
                    } // 전투불능 방지 상세 종료
                    else if (effect == BattleStatusEffectType.WandEcho) // 지팡이 반향의 고정 복제 비율 확인
                    { // 마법 상태 도감 표시 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, 50); // 전투와 같은 복제 피해율 표시
                    } // 마법 상태 도감 표시 종료
                    else if (effect == BattleStatusEffectType.ConfessionDamageMark) // 고해실 유물의 실제 표식 효과 확인
                    { // 표식 도감 표시 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, 3); // 전투와 같은 실제 유물 가산량 표기
                    } // 고해 표식 도감 표시 종료
                    else if (effect == BattleStatusEffectType.IncomingMagicDamageUp || effect == BattleStatusEffectType.EvasionDown) // 지팡이의 마법 취약과 회피 감소 설명 처리
                    { // 원본 카드 대표 수치 확인 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, 15); // 전투 툴팁과 같은 시트 수치 사용
                    } // 상태 도감 설명 처리 종료
                    else if (effect == BattleStatusEffectType.ManaConcentration) // 화면에 새로 표시되는 마력 집중의 상세 설명
                    { // 다음 지팡이 피해 카드의 원본 수치 조회
                        description = BattleStatusEffectInstance.GetDescription(effect, 4); // 카드와 도감의 추가 피해가 일치하는지 확인
                    } // 대기 중인 마력 집중 상태의 연결 종료
                    else if (effect == BattleStatusEffectType.StealthBreakDamageUp) // 은신이 풀린 뒤 실제 타격을 기다리는 상태
                    { // 별도 팔찌 상태 설명 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, 8); // 유물에 지정된 원본 피해 증가량 전달
                    } // 별도 팔찌 상태 설명 종료
                    else if (effect == BattleStatusEffectType.ArcaneWard || effect == BattleStatusEffectType.MagicAttackPowerUp || effect == BattleStatusEffectType.Shock) // 새 지팡이 상태의 실제 도감 등록
                    { // 상태의 시트 수치 전달 시작
                        int amount = effect == BattleStatusEffectType.ArcaneWard ? 6 : effect == BattleStatusEffectType.MagicAttackPowerUp ? 5 : 1; // 각 상태에 적용되는 원본 카드 수치
                        description = BattleStatusEffectInstance.GetDescription(effect, amount); // 전투와 도감의 실제 상태 설명 일치
                    } // 상태의 시트 수치 전달 종료
                    else if (effect == BattleStatusEffectType.StethoscopeMark) // 신규 청진기 원본의 도감 분류
                    { // 명칭과 표시 설명 준비
                        description = BattleStatusEffectInstance.GetDescription(effect, 1); // 실제 청진기 표식의 독립 설명
                    } // 도감 설명 준비 종료
                    else if (effect == BattleStatusEffectType.CorrosionResistanceDown || effect == BattleStatusEffectType.PulseCordAttackPowerUp || effect == BattleStatusEffectType.CrystalMarkDefenseDown) // 침식 전용 약화와 일시 공격력 조회
                    { // 침식 상태 기본 설명 준비 시작
                        int amount = effect == BattleStatusEffectType.CorrosionResistanceDown ? 5 : effect == BattleStatusEffectType.PulseCordAttackPowerUp ? 2 : 1; // 실제 시트 발동 수치
                        description = BattleStatusEffectInstance.GetDescription(effect, amount); // 전투와 같은 설명 문구 조회
                    } // 침식 상태 기본 설명 준비 종료
                    else if (effect == BattleStatusEffectType.ResurrectionSeed || effect == BattleStatusEffectType.PainDistribution || effect == BattleStatusEffectType.MiracleEcho) // 부활과 분담 및 회복 강화 상태를 각각 조회
                    { // 신규 성배 상태의 기본 수치 설명 시작
                        int amount = effect == BattleStatusEffectType.ResurrectionSeed ? 20 : effect == BattleStatusEffectType.PainDistribution ? 25 : 6; // 실제 원본 카드의 기본 수치 일치
                        description = BattleStatusEffectInstance.GetDescription(effect, amount); // 도감과 전투 툴팁의 같은 설명 사용
                    } // 신규 성배 상태의 기본 수치 설명 종료
                    else if (effect == BattleStatusEffectType.Stealth) // 독립 은신 상태 도감 항목
                    { // 은신 설명 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, 0); // 상태 원본의 고유 표시 문구
                    } // 은신 설명 종료
                    else if (effect == BattleStatusEffectType.SanctuaryWard || effect == BattleStatusEffectType.FearResistance || effect == BattleStatusEffectType.DelayedHealing) // 성역과 공포 저항 및 예약 회복 조회
                    { // 신규 성배 상태 설명 시작
                        int amount = effect == BattleStatusEffectType.SanctuaryWard ? 10 : effect == BattleStatusEffectType.FearResistance ? 30 : 6; // 각 성배 카드의 실제 상태 수치
                        description = BattleStatusEffectInstance.GetDescription(effect, amount); // 전투 상태와 같은 기본 도감 설명
                    } // 신규 성배 상태 설명 종료
                    else if (effect == BattleStatusEffectType.HealingReceivedUp) // 잔잔한 축복의 조건과 수치 확인
                    { // 받는 회복량 증가 설명 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, 20); // 실제 카드의 받는 회복량 기본 수치
                    } // 받는 회복량 증가 설명 종료
                    else if (effect == BattleStatusEffectType.Renewal) // 새 턴 종료 재생의 카드 효과 확인
                    { // 실제 턴 종료 회복 설명 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, 5); // 카드의 기본 턴 종료 회복량 표시
                    } // 실제 턴 종료 회복 설명 종료
                    else if (effect == BattleStatusEffectType.Vulnerable) // 검은 왕관의 받는 피해 증가 확인
                    { // 취약 설명 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, 20); // 실제 검은 왕관의 기본 취약 20퍼센트 설명
                    } // 취약 설명 종료
                    else if (effect == BattleStatusEffectType.PublicMark) // 수신기의 공명 표식 확인
                    { // 표식 설명 시작
                        description = BattleStatusEffectInstance.GetDescription(effect, 1); // 분필과 적용 기한 표시
                    } // 표식 설명 종료
                    Func<Sprite> icon = description.Length > 0 ? () => BattleStatusIconArt.Get(effect) : null; // 표시 중인 상태에만 사용할 작은 아이콘 조회
                    string key = icon != null ? "status_" + effect : null; // 상태별 독립 썸네일 키 설정
                    entries.Add(new CodexEntry(BattleStatusEffectInstance.GetDisplayName(effect), description, key, icon)); // 상태 이름과 설명 및 이미지 키 등록
                } // 상태 효과 추가 종료
            } // 상태 효과 확인 종료
        } // 상태 효과 수집 종료
        return entries; // 실제 원본 항목 반환
    } // 도감 항목 조회 종료
    private static Row[] LoadRows() // 원본 에셋 없이 도감 문자 목록 파싱
    { // 도감 목록 조회 시작
        if (rows == null) // 회차에서 처음으로 사용하는 도감 원본 확인
        { // 텍스트 자료 준비 시작
            TextAsset text = Resources.Load<TextAsset>("Codex/Index"); // 에셋 참조가 없는 JSON 텍스트 조회
            rows = text != null ? JsonUtility.FromJson<Index>(text.text)?.rows ?? Array.Empty<Row>() : Array.Empty<Row>(); // 이미지 오브젝트 없는 목록 한 번 파싱
        } // 텍스트 자료 준비 종료
        return rows; // 현재 도감 텍스트 조회 결과 반환
    } // 도감 목록 조회 종료
} // 도감 데이터 종료
