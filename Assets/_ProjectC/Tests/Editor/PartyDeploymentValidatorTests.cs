using System.Collections.Generic; // 테스트 파티 목록 사용
using System.Reflection; // ScriptableObject 테스트 데이터 주입
using NUnit.Framework; // Unity Editor 테스트 기능 사용
using UnityEngine; // 테스트 ScriptableObject 생성·정리

public sealed class PartyDeploymentValidatorTests // 58일차 파티 출전 규칙 테스트
{
    private sealed class FakeStatusProvider : IPartyDeploymentStatusProvider // 테스트 출전 상태 공급자
    {
        private readonly HashSet<CharacterData> deadCharacters =
            new HashSet<CharacterData>(); // 사망 캐릭터 목록

        private readonly HashSet<CharacterData> recoveringCharacters =
            new HashSet<CharacterData>(); // 회복 중 캐릭터 목록

        private readonly HashSet<CharacterData> unavailableCharacters =
            new HashSet<CharacterData>(); // 기타 출전 불가 캐릭터 목록

        public void SetDead(CharacterData characterData) // 사망 상태 지정
        {
            deadCharacters.Add(characterData); // 사망 목록 등록
        }

        public void SetRecovering(CharacterData characterData) // 회복 상태 지정
        {
            recoveringCharacters.Add(characterData); // 회복 목록 등록
        }

        public void SetUnavailable(CharacterData characterData) // 기타 출전 불가 상태 지정
        {
            unavailableCharacters.Add(characterData); // 출전 불가 목록 등록
        }

        public bool IsDead(CharacterData characterData) // 테스트 사망 여부 조회
        {
            return deadCharacters.Contains(characterData); // 사망 목록 포함 반환
        }

        public bool IsRecovering(CharacterData characterData) // 테스트 회복 여부 조회
        {
            return recoveringCharacters.Contains(characterData); // 회복 목록 포함 반환
        }

        public bool CanDeploy(CharacterData characterData) // 테스트 출전 가능 여부 조회
        {
            return !deadCharacters.Contains(characterData) &&
                   !recoveringCharacters.Contains(characterData) &&
                   !unavailableCharacters.Contains(characterData); // 모든 차단 상태 제외 반환
        }
    }

    private readonly List<Object> createdObjects =
        new List<Object>(); // 테스트 생성 객체 정리 목록

    [TearDown]
    public void TearDown() // 테스트 객체 정리
    {
        if (BattleResultManager.Instance != null)
        {
            BattleResultManager.Instance.ResetSavedPartyState(); // 저장 파티 상태 초기화
        }

        if (CharacterRecoveryManager.Instance != null)
        {
            CharacterRecoveryManager.Instance.ResetRecoveryState(); // 회복 상태 초기화
        }

        foreach (Object createdObject in createdObjects)
        {
            if (createdObject != null)
            {
                Object.DestroyImmediate(createdObject); // 생성 ScriptableObject 제거
            }
        }

        createdObjects.Clear(); // 정리 목록 초기화
    }

    [Test]
    public void ValidLivingParty_CanDeployAllMembers() // 정상 생존 파티 전체 출전 확인
    {
        CharacterData first = CreateCharacter("ally_a", "아군 A"); // 첫 캐릭터 생성
        CharacterData second = CreateCharacter("ally_b", "아군 B"); // 둘째 캐릭터 생성
        PartyData party = CreateParty(first, second); // 2인 파티 생성
        FakeStatusProvider provider = new FakeStatusProvider(); // 전원 생존 상태 공급자 생성

        PartyDeploymentValidationResult result =
            PartyDeploymentValidator.Validate(party, provider); // 파티 출전 검증

        Assert.IsTrue(result.CanDeploy); // 전체 출전 가능 확인
        Assert.AreEqual(PartyDeploymentBlockReason.None, result.BlockReason); // 차단 없음 확인
        Assert.AreEqual(2, result.DeployableMemberCount); // 출전 가능 2명 확인
        Assert.IsNull(result.BlockedCharacter); // 차단 캐릭터 없음 확인
    }

    [Test]
    public void DeadMember_BlocksPartyDeployment() // 사망 캐릭터 파티 편성 차단 확인
    {
        CharacterData first = CreateCharacter("ally_a", "아군 A"); // 생존 캐릭터 생성
        CharacterData dead = CreateCharacter("ally_dead", "사망 아군"); // 사망 캐릭터 생성
        PartyData party = CreateParty(first, dead); // 사망 포함 파티 생성
        FakeStatusProvider provider = new FakeStatusProvider(); // 테스트 상태 공급자 생성
        provider.SetDead(dead); // 사망 상태 지정

        PartyDeploymentValidationResult result =
            PartyDeploymentValidator.Validate(party, provider); // 파티 출전 검증

        Assert.IsFalse(result.CanDeploy); // 파티 출전 차단 확인
        Assert.AreEqual(PartyDeploymentBlockReason.DeadMember, result.BlockReason); // 사망 사유 확인
        Assert.AreSame(dead, result.BlockedCharacter); // 차단 캐릭터 확인
        Assert.AreEqual(1, result.DeployableMemberCount); // 사망 전 생존 인원 집계 확인
    }

    [Test]
    public void RecoveringMember_BlocksBeforeDeadReason() // 회복 중 캐릭터 우선 차단 확인
    {
        CharacterData recovering = CreateCharacter("ally_recovering", "회복 아군"); // 회복 캐릭터 생성
        PartyData party = CreateParty(recovering); // 회복 캐릭터 파티 생성
        FakeStatusProvider provider = new FakeStatusProvider(); // 테스트 상태 공급자 생성
        provider.SetDead(recovering); // 저장 HP 사망 상태 지정
        provider.SetRecovering(recovering); // 회복 설비 상태 지정

        PartyDeploymentValidationResult result =
            PartyDeploymentValidator.Validate(party, provider); // 파티 출전 검증

        Assert.IsFalse(result.CanDeploy); // 파티 출전 차단 확인
        Assert.AreEqual(PartyDeploymentBlockReason.RecoveringMember, result.BlockReason); // 회복 중 사유 우선 확인
        Assert.AreSame(recovering, result.BlockedCharacter); // 회복 캐릭터 차단 확인
    }

    [Test]
    public void UnavailableMember_UsesFallbackBlockReason() // 기타 출전 불가 상태 차단 확인
    {
        CharacterData unavailable = CreateCharacter("ally_locked", "잠금 아군"); // 출전 불가 캐릭터 생성
        PartyData party = CreateParty(unavailable); // 출전 불가 파티 생성
        FakeStatusProvider provider = new FakeStatusProvider(); // 테스트 상태 공급자 생성
        provider.SetUnavailable(unavailable); // 기타 출전 불가 지정

        PartyDeploymentValidationResult result =
            PartyDeploymentValidator.Validate(party, provider); // 파티 출전 검증

        Assert.IsFalse(result.CanDeploy); // 파티 출전 차단 확인
        Assert.AreEqual(PartyDeploymentBlockReason.UnavailableMember, result.BlockReason); // 기타 차단 사유 확인
    }

    [Test]
    public void SavedState_ReRegisterParty_PreservesHealthAndMental() // 탐사 재등록 시 HP·정신력 유지 확인
    {
        CharacterData character = CreateCharacter("ally_persist", "상태 유지 아군"); // 상태 유지 캐릭터 생성
        PartyData party = CreateParty(character); // 상태 유지 파티 생성
        BattleResultManager resultManager = BattleResultManager.EnsureInstance(); // 저장 상태 관리자 준비
        resultManager.ResetSavedPartyState(); // 이전 테스트 저장 상태 초기화

        Assert.IsTrue(resultManager.RegisterParty(party)); // 최초 파티 등록 확인
        Assert.AreEqual(1, resultManager.ApplyExplorationHazardToActiveParty(30, 20)); // HP·정신력 감소 적용
        Assert.IsTrue(resultManager.TryGetSavedAllyState(
            character,
            out int firstHealth,
            out int firstMental,
            out int firstDeathCount)); // 최초 저장 상태 조회

        Assert.AreEqual(70, firstHealth); // 감소 후 HP 확인
        Assert.AreEqual(30, firstMental); // 감소 후 정신력 확인
        Assert.AreEqual(0, firstDeathCount); // 사망 횟수 없음 확인

        Assert.IsTrue(resultManager.RegisterParty(party)); // 같은 파티 재등록 확인
        Assert.IsTrue(resultManager.TryGetSavedAllyState(
            character,
            out int secondHealth,
            out int secondMental,
            out int secondDeathCount)); // 재등록 후 저장 상태 조회

        Assert.AreEqual(firstHealth, secondHealth); // 재등록 시 HP 초기화 방지 확인
        Assert.AreEqual(firstMental, secondMental); // 재등록 시 정신력 초기화 방지 확인
        Assert.AreEqual(firstDeathCount, secondDeathCount); // 재등록 시 사망 횟수 유지 확인
    }

    [Test]
    public void SavedDeadState_IsBlockedAndRecoveryStateHasPriority() // 실제 저장 사망·회복 상태 출전 차단 확인
    {
        CharacterData character = CreateCharacter("ally_runtime_dead", "런타임 사망 아군"); // 런타임 사망 캐릭터 생성
        PartyData party = CreateParty(character); // 런타임 사망 파티 생성
        BattleResultManager resultManager = BattleResultManager.EnsureInstance(); // 저장 상태 관리자 준비
        CharacterRecoveryManager recoveryManager = CharacterRecoveryManager.EnsureInstance(); // 회복 관리자 준비
        resultManager.ResetSavedPartyState(); // 이전 저장 상태 초기화
        recoveryManager.ResetRecoveryState(); // 이전 회복 상태 초기화

        Assert.IsTrue(resultManager.RegisterParty(party)); // 파티 등록 확인
        Assert.AreEqual(0, resultManager.ApplyExplorationHazardToActiveParty(999, 0)); // 저장 HP 0 사망 처리

        CharacterRecoveryDeploymentStatusProvider provider =
            new CharacterRecoveryDeploymentStatusProvider(recoveryManager); // 실제 회복 시스템 상태 공급자 생성

        PartyDeploymentValidationResult deadResult =
            PartyDeploymentValidator.Validate(party, provider); // 사망 상태 출전 검증

        Assert.IsFalse(deadResult.CanDeploy); // 사망 캐릭터 출전 차단 확인
        Assert.AreEqual(PartyDeploymentBlockReason.DeadMember, deadResult.BlockReason); // 사망 차단 사유 확인
        Assert.IsTrue(resultManager.IsActivePartyWiped()); // 저장 상태 기준 파티 전멸 확인

        Assert.IsTrue(recoveryManager.RegisterDeadCharacter(character, resultManager)); // 사망 캐릭터 회복 등록 확인

        PartyDeploymentValidationResult recoveringResult =
            PartyDeploymentValidator.Validate(party, provider); // 회복 상태 출전 재검증

        Assert.IsFalse(recoveringResult.CanDeploy); // 회복 중 출전 차단 확인
        Assert.AreEqual(PartyDeploymentBlockReason.RecoveringMember, recoveringResult.BlockReason); // 회복 중 사유 우선 확인
    }

    [Test]
    public void InvalidParty_IsRejectedBeforeRuntimeStateCheck() // 잘못된 파티 우선 차단 확인
    {
        PartyDeploymentValidationResult result =
            PartyDeploymentValidator.Validate(null, new FakeStatusProvider()); // 빈 파티 검증

        Assert.IsFalse(result.CanDeploy); // 빈 파티 출전 차단 확인
        Assert.AreEqual(PartyDeploymentBlockReason.InvalidParty, result.BlockReason); // 파티 오류 사유 확인
        Assert.AreEqual(0, result.DeployableMemberCount); // 출전 가능 인원 0명 확인
    }

    private CharacterData CreateCharacter(
        string characterId,
        string displayName) // 테스트 캐릭터 생성
    {
        CharacterData characterData =
            ScriptableObject.CreateInstance<CharacterData>(); // 캐릭터 ScriptableObject 생성

        SetPrivateField(characterData, "characterId", characterId); // 캐릭터 ID 설정
        SetPrivateField(characterData, "displayName", displayName); // 캐릭터 이름 설정
        SetPrivateField(characterData, "maxHealth", 100); // 최대 HP 설정
        SetPrivateField(characterData, "initialMental", 50); // 초기 정신력 설정
        createdObjects.Add(characterData); // 정리 목록 등록
        return characterData; // 생성 캐릭터 반환
    }

    private PartyData CreateParty(params CharacterData[] members) // 테스트 파티 생성
    {
        PartyData partyData =
            ScriptableObject.CreateInstance<PartyData>(); // 파티 ScriptableObject 생성

        SetPrivateField(partyData, "partyId", "test_party"); // 파티 ID 설정
        SetPrivateField(partyData, "displayName", "테스트 파티"); // 파티 이름 설정
        SetPrivateField(
            partyData,
            "members",
            new List<CharacterData>(members)); // 파티원 목록 설정

        createdObjects.Add(partyData); // 정리 목록 등록
        return partyData; // 생성 파티 반환
    }

    private static void SetPrivateField(
        object target,
        string fieldName,
        object value) // 비공개 직렬화 필드 테스트 값 설정
    {
        FieldInfo field =
            target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic); // 대상 비공개 필드 조회

        Assert.IsNotNull(field, $"테스트 필드를 찾을 수 없습니다: {fieldName}"); // 필드 존재 확인
        field.SetValue(target, value); // 테스트 값 주입
    }
}
