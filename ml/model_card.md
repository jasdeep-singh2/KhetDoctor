# KhetDoctor Plant Disease Model v1
- Date: 2026-10-31
- Architecture: EfficientNet-B0 (ImageNet pretrained), 13 classes
- Data: PlantVillage color (tomato + potato), 70/15/15 stratified split, seed 42
- Preprocessing: RGB, resize 224x224 (stretch), /255, ImageNet mean/std, NCHW
- Temperature: 0.6923
- Test (PlantVillage): accuracy 0.993, macro-F1 0.993
- Field test (PlantDoc): accuracy 0.247 (21/85)

## Known confusions
- Tomato___Bacterial_spot → predicted as Tomato___Target_Spot (4x)
- Tomato___Tomato_Yellow_Leaf_Curl_Virus → predicted as Tomato___Bacterial_spot (3x)
- Tomato___Septoria_leaf_spot → predicted as Tomato___Early_blight (2x)

## Limitations
- Lab images; not trained on other crops; always outputs one of 13 classes
