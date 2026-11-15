using System.Collections.Generic; // 스프라이트 중복 검사 집합 사용
using NUnit.Framework; // 카드 이미지 연결 검증 사용
using UnityEngine; // 실제 카드와 스프라이트 리소스 사용
public sealed class CardIllustrationTests // 시작 카드 일러스트 연결 검사
{ // 일러스트 검사 시작
    [Test] // 시작 카드별 독립 일러스트 검사 지정
    public void StarterCards_HaveDistinctIllustrations() // 전용 카드와 공용 카드의 그림 연결 확인
    { // 카드 일러스트 검사 시작
        CardData[] cards = Resources.LoadAll<CardData>("StarterCards"); // 실제 시작 카드 전체 조회
        Assert.AreEqual(26, cards.Length); // 전용 카드 스물두 종과 공용 카드 네 종 확인
        HashSet<Sprite> images = new HashSet<Sprite>(); // 이미 사용한 스프라이트 집합 생성
        foreach (CardData card in cards) // 실제 시작 카드 순회
        { // 카드 그림 확인 시작
            Assert.IsNotNull(card.Artwork, card.CardId); // 카드별 일러스트 연결 확인
            Assert.IsTrue(card.Artwork.rect.width > 0 && card.Artwork.rect.height > 0, card.CardId); // 스프라이트 이미지 크기 확인
            Assert.IsTrue(images.Add(card.Artwork), card.CardId); // 서로 다른 카드의 스프라이트 중복 없음 확인
        } // 카드 그림 확인 종료
    } // 카드 일러스트 검사 종료
} // 일러스트 검사 종료
