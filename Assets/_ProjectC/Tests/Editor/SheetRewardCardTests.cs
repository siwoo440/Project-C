using System.Collections.Generic; // 보상 후보 목록 사용
using System.Reflection; // 실제 보상 후보 조회 사용
using NUnit.Framework; // 카드 동작 검사 사용
using UnityEditor; // 실제 카드 에셋 조회 사용
using UnityEngine; // 유니티 데이터 사용

public sealed class SheetRewardCardTests // 시트 보상 카드 검사
{ // 카드 검사 시작
    [Test] // 실제 시트 카드 검사 지정
    public void CardsHaveSheetValuesArtworkAndRewardAccess() // 카드 수치와 실제 획득 경로 확인
    { // 카드 검사 본문 시작
        string[] names = { "작은 치유", "불덩이", "마력탄" }; // 확인할 시트 카드 이름
        string[] paths = { "Assets/_ProjectC/ScriptableObjects/Cards/Card_SheetSmallHeal.asset", "Assets/_ProjectC/ScriptableObjects/Cards/Card_SheetFireball.asset", "Assets/_ProjectC/ScriptableObjects/Cards/Card_SheetMagicMissile.asset" }; // 실제 카드 에셋 경로
        int[] values = { 6, 9, 7 }; // 시트 회복과 피해 수치
        CardType[] types = { CardType.Cup, CardType.Wand, CardType.Wand }; // 시트 카드 속성
        CardTargetType[] targets = { CardTargetType.SingleAlly, CardTargetType.SingleEnemy, CardTargetType.SingleEnemy }; // 시트 단일 대상
        CardEffectType[] effects = { CardEffectType.Heal, CardEffectType.Damage, CardEffectType.Damage }; // 시트 카드 효과
        SaveAssetCatalog save = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 저장 카드 원본 조회
        Assert.IsNotNull(save); // 저장 원본 존재 확인
        MethodInfo candidateMethod = typeof(ExplorationTreasureRewardService).GetMethod("GetValidOffers", BindingFlags.Static | BindingFlags.NonPublic); // 실제 보물 후보 조회
        List<ShopOfferData> candidates = (List<ShopOfferData>)candidateMethod.Invoke(null, new object[] { ShopOfferType.Card }); // 실제 보물 카드 후보 계산
        for (int index = 0; index < names.Length; index++) // 세 카드 순회
        { // 카드 검증 시작
            CardData card = AssetDatabase.LoadAssetAtPath<CardData>(paths[index]); // 실제 카드 에셋 조회
            Assert.IsNotNull(card, names[index]); // 카드 에셋 존재 확인
            Assert.AreEqual(names[index], card.DisplayName); // 시트 이름 확인
            Assert.AreEqual(1, card.ApCost); // 시트 행동력 비용 확인
            Assert.AreEqual(values[index], card.EffectValue); // 시트 효과 수치 확인
            Assert.AreEqual(types[index], card.CardType); // 시트 카드 속성 확인
            Assert.AreEqual(targets[index], card.TargetType); // 시트 카드 대상 확인
            Assert.AreEqual(effects[index], card.EffectType); // 시트 카드 효과 확인
            Assert.IsNotNull(card.Artwork); // 원본 카드 이미지 확인
            Assert.Contains(card, (System.Collections.ICollection)save.Cards); // 저장 카드 원본 등록 확인
            Assert.IsTrue(candidates.Exists(offer => offer.Card == card && offer.RequiresOwnerChoice)); // 파티원 선택 보물 후보 확인
            if (index > 0) // 마법 공격 카드 확인
            { // 마법 피해 검사 시작
                Assert.AreEqual(BattleDamageType.Magical, card.DamageType); // 시트 마법 피해 유형 확인
            } // 마법 피해 검사 종료
        } // 카드 검증 종료
    } // 카드 검사 본문 종료
} // 카드 검사 종료
