# env
# pytorch 1.3.1
# numpy 1.19.2


import numpy as np
import torch
import torch.nn as nn
import torch.optim as optim
from torch.utils.data import DataLoader

import numpy as np
import os
from sklearn.model_selection import TimeSeriesSplit
from model import *
import utils
from datetime import datetime

inSize = 7
learningRate = 0.001
epoch = 100
batchSize = 64
n_hidden = 64
outSize = 2
sequence_length = 50
k_fold = 5
layerNum = 2

timePeriod = 1 # @@@ sec, must Synchronized to Unity !!
# k_fold reference https://github.com/christianversloot/machine-learning-articles/blob/main/how-to-use-k-fold-cross-validation-with-pytorch.md
print(torch.__version__)

"""
def k_Fold_CrossValid(dataSet, k, fold):
    length = len(dataSet)
    foldSize = (int)(length/fold)
    trainSet = {}
    validSet = {}
    for i in range (0,fold):
        if i == k :
            validSet = dataSet[k*foldSize:(k+1)*foldSize, :]
        else:
            trainSet = dataSet[i*foldSize:((i+1)*foldSize), :]

    return trainSet, validSet"""

utils.createFolder("./model")

def kFoldValidation(dataSet):
    trainSet = {}
    validSet = {}
    for train_index, test_index in enumerate(kFold.split(dataSet)):
        trainSet = dataSet[train_index]
        validSet = dataSet[test_index]

    return trainSet, validSet


modelResultF = open("./model/model Result.txt", 'w')
modelResultF.write("--------------------------------------------------\n Hyper Parameter ...\n")
modelResultF.write("input Size : {}\n".format(inSize))
modelResultF.write("output Size : {}\n".format(outSize))
modelResultF.write("Layer Number : {}\n".format(layerNum))
modelResultF.write("hidden Unit : {}\n".format(n_hidden))
modelResultF.write("batch Size : {}\n".format(batchSize))
modelResultF.write("Epoch : {}\n".format(epoch))
modelResultF.write("Data Sequence Length : {}\n".format(sequence_length))
modelResultF.write("Learning Rate : {}\n".format(learningRate))
modelResultF.write("prediction Period : {} sec\n".format(timePeriod))
modelResultF.write("--------------------------------------------------\n")

# @@@ Please set DataBuilder mode depending on Model, (LSTM, GRU : mode = 1, Transformer : mode = 2)
dataBuilder = utils.DataBuilder("./data", ".txt",mode=2)

dataSource = dataBuilder.getDatas()

trainDataSize = (int)(len(dataSource) * 4 / 5)

Data = dataSource[:trainDataSize, :]
testData = dataSource[trainDataSize:, :]
trainX, trainY = dataBuilder.buildDataSet(Data, sequence_length, inSize, timePeriod)
testX, testY = dataBuilder.buildDataSet(testData, sequence_length, inSize, timePeriod)



trainDataSet = utils.timeseries(trainX, trainY)
testDataSet = utils.timeseries(testX, testY)


trainLoader = DataLoader(trainDataSet, batch_size=batchSize, shuffle=False, drop_last=True)  # data loader
testLoader = DataLoader(testDataSet, batch_size=batchSize, shuffle=False, drop_last=True)

print("trainData : " + str(trainX[0,:]))
print("targetData : " + str(trainY[0, :]))
print("total Data shape ... " + str(dataSource.shape))
print("trainData Shape" + str(trainX.shape))
print("testData Shape" + str(testX.shape))
#print("label : " + str(label[0]))



device = torch.device("cuda:0" if torch.cuda.is_available() else "cpu")
print('device : ' + str(device))

lossFunction = nn.MSELoss()

# kFold = KFold(n_splits=k_fold, shuffle=False)
kFold = TimeSeriesSplit(n_splits=k_fold)
result = {}
model = {}
# training
loadOrder = input("Load model ?[yes : y / no : any] ..>")
if loadOrder != 'y':

    for fold, (train_ids, test_ids) in enumerate(kFold.split(trainDataSet)):
        print("--------------------------------------------\n")
        print("Fold {}\n".format(fold))
        print('train_ids {} , test_ids {}  \n'.format(train_ids, test_ids))

        trainSet = utils.timeseries(trainDataSet.x[train_ids], trainDataSet.y[train_ids])
        validSet = utils.timeseries(trainDataSet.x[test_ids], trainDataSet.y[test_ids])

        subTrainLoader = torch.utils.data.DataLoader(trainSet, batch_size=batchSize, shuffle=False)
        subValidLoader = torch.utils.data.DataLoader(validSet, batch_size=batchSize, shuffle=False)
        model[fold] = Transformer(num_tokens=2, dim_model=4, num_heads=1, num_encoder_layers=1, num_decoder_layers=1,dim_feedforward=n_hidden, dropout_p=0.3, loss_function=lossFunction,learning_rate=learningRate, device=device)

        #model[fold] = LSTMModel(inSize, n_hidden, outSize, batchSize, epoch, sequence_length, device, lossFunction,learningRate, layerNum).to(device)
        optimizer = optim.Adam(model[fold].parameters(), lr=learningRate)
        model[fold].Training(subTrainLoader)
        print("\n Training process has finished Saving model...\n")
        torch.save({'state_dict': model[fold].state_dict()}, './model/model_Fold{}.pkl'.format(fold))
        result[fold] = model[fold].evaluate(subValidLoader, 'validation')
        modelResultF.write("model_Fold_{} validation Accuracy : {}\n".format(fold, result[fold]))

    print(f'K-FOLD CROSS VALIDATION RESULTS FOR {k_fold} FOLDS')
    print('--------------------------------')
    sum = 0.0
    for key, value in result.items():
        print(f'Fold {key}: {value} %')
        sum += value
    avgAccuracy = sum / len(result.items())
    print(f'Average: {avgAccuracy}')
    modelResultF.write("Average Model validation Accuracy : {}\n".format(avgAccuracy))
    try:
        os.mkdir("./model")
    except:
        pass

else:
    for k in range(0, k_fold):
        modelState = torch.load('./model/model_Fold{}.pkl'.format(k))
        #model[k] = LSTMModel(inSize, n_hidden, outSize, batchSize, epoch, sequence_length, device, lossFunction, learningRate, layerNum).to(device)
        model[k] = GRUModel(inSize, n_hidden, outSize, batchSize, epoch, sequence_length, device, lossFunction,learningRate, layerNum).to(device)
        #model[k] = TransformerModel(inSize, n_hidden, outSize, batchSize, epoch, sequence_length, device, lossFunction,learningRate, layerNum).to(device)
        model[k].load_state_dict(modelState['state_dict'])

print("----------------------------------\n testing model ...\n")

modelResultF.write("-------------------------------------------\n")
for j in range(0, k_fold):
    acc = model[j].Pos_evaluate(testLoader, 'test Fold{}'.format(j))
    modelResultF.write("model_Fold_{} Test Dataset Accuracy : {}\n".format(j, acc))

modelResultF.write("-------------------------------------------\n")
for j in range(0, k_fold):
    acc = model[j].Pos_evaluate(trainLoader, 'train Fold{}'.format(j))
    modelResultF.write("model_Fold_{} Train Dataset Accuracy : {}\n".format(j, acc))






