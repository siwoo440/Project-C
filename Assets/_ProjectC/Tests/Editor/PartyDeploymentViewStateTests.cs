using NUnit.Framework; // Unity Editor 테스트 기능 사용

public sealed class PartyDeploymentViewStateTests // 58일차 Part 2 파티 출전 표시 상태 테스트
{
    private sealed class FakeStatusProvider : IPartyDeploymentStatusProvider // 테스트용 출전 상태 제공자
    {
        public bool Dead; // 사망 상태
        public bool Recovering; // 회복 상태
        public bool Deployable = true; // 출전 가능 상태

        public bool IsDead(CharacterData characterData)
        {
            return Dead; // 테스트 사망 상태 반환
        }

        public bool IsRecovering(CharacterData characterData)
        {
            return Recovering; // 테스트 회복 상태 반환
        }

        public bool CanDeploy(CharacterData characterData)
        {
            return Deployable; // 테스트 출전 가능 상태 반환
        }
    }

    [Test]
    public void AvailableMember_IsSelectableAndNotDimmed() // 정상 캐릭터 선택 가능 상태 확인
    {
        PartyMemberDeploymentViewState result = PartyDeploymentViewStateFactory.Create(
            null,
            new FakeStatusProvider(),
            0); // 정상 상태 생성

        Assert.AreEqual(PartyMemberDeploymentState.Available, result.State); // 출전 가능 상태 확인
        Assert.IsTrue(result.CanSelect); // 선택 가능 확인
        Assert.IsFalse(result.IsDimmed); // 흐림 없음 확인
        Assert.AreEqual("출전 가능", result.StatusText); // 상태 문구 확인
    }

    [Test]
    public void DeadMember_IsBlockedAndDimmed() // 사망 캐릭터 선택 차단 확인
    {
        PartyMemberDeploymentViewState result = PartyDeploymentViewStateFactory.Create(
            null,
            new FakeStatusProvider
            {
                Dead = true,
                Deployable = false
            },
            0); // 사망 상태 생성

        Assert.AreEqual(PartyMemberDeploymentState.Dead, result.State); // 사망 상태 확인
        Assert.IsFalse(result.CanSelect); // 선택 차단 확인
        Assert.IsTrue(result.IsDimmed); // 흐림 표시 확인
        Assert.AreEqual("사망", result.StatusText); // 사망 문구 확인
    }

    [Test]
    public void RecoveringMember_ShowsRemainingExpeditions() // 회복 캐릭터 남은 탐사 표시 확인
    {
        PartyMemberDeploymentViewState result = PartyDeploymentViewStateFactory.Create(
            null,
            new FakeStatusProvider
            {
                Dead = true,
                Recovering = true,
                Deployable = false
            },
            2); // 회복 상태 생성

        Assert.AreEqual(PartyMemberDeploymentState.Recovering, result.State); // 회복 상태 우선 확인
        Assert.IsFalse(result.CanSelect); // 선택 차단 확인
        Assert.IsTrue(result.IsDimmed); // 흐림 표시 확인
        Assert.AreEqual("회복 중 · 2회", result.StatusText); // 남은 탐사 문구 확인
    }

    [Test]
    public void UnavailableMember_UsesFallbackBlockedState() // 기타 출전 불가 상태 확인
    {
        PartyMemberDeploymentViewState result = PartyDeploymentViewStateFactory.Create(
            null,
            new FakeStatusProvider
            {
                Deployable = false
            },
            0); // 기타 차단 상태 생성

        Assert.AreEqual(PartyMemberDeploymentState.Unavailable, result.State); // 기타 출전 불가 상태 확인
        Assert.IsFalse(result.CanSelect); // 선택 차단 확인
        Assert.AreEqual("출전 불가", result.StatusText); // 차단 문구 확인
    }
}
