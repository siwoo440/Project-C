using NUnit.Framework; // 테스트 기능 사용
using UnityEngine; // 리소스 조회 기능 사용
public sealed class StarterCardResourcesTests // 역할별 임시 시작 카드 테스트
{ // 테스트 클래스 시작
    [Test] // 테스트 지정
    public void StarterCards_LoadForEveryPartyRole() // 모든 파티 역할의 시작 카드 확인
    { // 테스트 시작
        string[] names = { "Attack", "Defense", "Support", "Strategy" }; // 역할별 리소스 이름
        CardEffectType[] effects = { CardEffectType.Damage, CardEffectType.ApplyStatusEffect, CardEffectType.Heal, CardEffectType.Damage }; // 역할별 임시 효과
        for (int index = 0; index < names.Length; index++) // 네 가지 역할 순회
        { // 역할 카드 확인 시작
            CardData card = Resources.Load<CardData>($"StarterCards/{names[index]}"); // 임시 시작 카드 조회
            Assert.IsNotNull(card, names[index]); // 역할 카드 존재 확인
            Assert.AreEqual(effects[index], card.EffectType); // 카드 효과 종류 확인
            Assert.Greater(card.EffectValue, 0); // 양수 임시 효과 확인
            Assert.IsNotNull(card.Artwork); // 시작 카드 일러스트 연결 확인
        } // 역할 카드 확인 종료
    } // 테스트 종료
    [Test] // 테스트 지정
    public void MagicianCard_UsesItsOwnIllustration() // 마법사 전용 일러스트 연결 확인
    { // 테스트 시작
        CardData magician = Resources.Load<CardData>("StarterCards/MagicianRepair"); // 마법사 전용 카드 조회
        CardData common = Resources.Load<CardData>("StarterCards/Strategy"); // 공용 전략 카드 조회
        Assert.IsNotNull(magician); // 마법사 카드 존재 확인
        Assert.IsNotNull(common); // 공용 카드 존재 확인
        Assert.IsNotNull(magician.Artwork); // 마법사 일러스트 연결 확인
        Assert.AreNotSame(common.Artwork, magician.Artwork); // 공용 카드와 서로 다른 일러스트 확인
        Assert.AreEqual("MagicianRepairArt", magician.Artwork.texture.name); // 전용 이미지 원본 확인
    } // 테스트 종료
} // 테스트 클래스 종료
