from torch.utils.data import Dataset
import numpy as np
import math
import glob
import os
from abc import *
import torch


def createFolder(directory):
    try:
        if not os.path.exists(directory):
            os.makedirs(directory)
    except OSError:
        print('Error: Creating directory. ' + directory)


def label_data(Data, target, labelThreshold):
    print("initiate label ...")

    label = np.zeros((len(Data), 3), dtype = int)
    for i in range(0, len(Data)):
        if target[i][0] < -labelThreshold:
            label[i, 0] = 1
        elif target[i][0] > labelThreshold:
            label[i, 2] = 1
        else:
            label[i, 1] = 1
    print("done\n")
    return label

def unit_vector(vector):
    """ Returns the unit vector of the vector.  """
    return vector / np.linalg.norm(vector)


class DataBuilder():
    def __init__(self, filePath, fileExtension = '.txt', mode = 0):
        self.dataSet = np.empty(0)
        self.path = filePath
        self.fileExtension = fileExtension
        self.setBuildFuntion(mode)

        try:
            os.mkdir(filePath)
        except:
            pass

        self.gatherData()

    def setBuildFuntion(self, mode = 0):
        if (mode == 0):
            self.dataBuildFunction = build__dataset_default()
        elif mode == 1:
            self.dataBuildFunction = build__dataset_fromPos()
        elif mode == 2:
            self.dataBuildFunction = build_Seqdataset_fromPosDataset()
        else:
            self.dataBuildFunction = build__dataset_fromPos_withDir()

    def gatherData(self):
        count = 1
        for filename in glob.glob(self.path + '/*' + self.fileExtension):
            print("filename : " + filename)
            if(count == 1):
                self.dataSet = np.loadtxt(filename, delimiter=',', dtype=np.float32)
                count = count - 1
            else:

                self.dataSet = np.vstack([self.dataSet,np.loadtxt(filename, delimiter=',', dtype=np.float32)] )

        return self.dataSet

    def getDatas(self):
        return self.dataSet

    def buildDataSet(self, time_series, seq_length, inSize, time = 1):
        return self.dataBuildFunction.build_dataSet(time_series, seq_length, inSize, time)

class SingleDataBuilder(DataBuilder):
    def __init__(self, filename, mode = 0):
        self.dataSet = np.empty(0)
        self.file = filename
        if (mode == 0):
            self.dataBuildFunction = build__dataset_fromPos()
        else:
            self.dataBuildFunction = build__dataset_fromPos_withDir()

    def gatherData(self):
        self.dataSet = np.loadtxt(self.file, delimiter=',', dtype=np.float32)

    def setFile(self, filename):
        self.file = filename
        self.gatherData()


def rotation_2D_vector(vector, angle):
    newVector = []
    radian = angle * (math.pi / 180)
    oldX = vector[0]
    oldY = vector[1]
    newX = oldX * math.cos(radian) - oldY * math.sin(radian)
    newY = (oldX * math.sin(radian)) + (oldY * math.cos(radian))
    newVector.append(newX)
    newVector.append(newY)
    return np.array(newVector, dtype='f')

class BuildDatasetBaseClass():

    @abstractmethod
    def build_dataSet(self,time_series, seq_length, inSize, time):
        pass

class build_Seqdataset_fromPosDataset(BuildDatasetBaseClass):

    def build_dataSet(self,time_series, seq_length, inSize, time):
        dataX = []
        dataY = []
        term = time * seq_length
        for i in range(0, len(time_series) - (int)(term * 2)):
            _x = time_series[range(i, i + term, time), :inSize]
            currentY = time_series[range(i, i + term), inSize:]
            futureY = time_series[range(i + term, i + (int)(term * 2)), inSize:]
            angle = time_series[range(i,i + term), 2]
            _y = futureY - currentY
            target =[]
            for k in range(0, len(currentY)):

                target.append(rotation_2D_vector(_y[k], angle[k]))
            # print(_x, "-->",_y)
            dataX.append(_x)
            # dataY.append(_y)
            dataY.append(target)
        return np.array(dataX), np.array(dataY)

class build__dataset_fromPos(BuildDatasetBaseClass):

    def build_dataSet(self,time_series, seq_length, inSize, time ):
        dataX = []
        dataY = []
        term = time * seq_length
        for i in range(0, len(time_series) - (int)(term * 2)):
            _x = time_series[range(i, i + term, time), :inSize]
            currentY = time_series[i + term - 1, inSize:]
            futureY = time_series[i + (int)(term * 2) - 1, inSize:]
            angle = time_series[i + term - 1, 2]
            _y = futureY - currentY
            target = rotation_2D_vector(_y, angle)
            # print(_x, "-->",_y)
            dataX.append(_x)
            # dataY.append(_y)
            dataY.append(target)
        return np.array(dataX), np.array(dataY)

class build__dataset_default(BuildDatasetBaseClass):

    def build_dataSet(self,time_series, seq_length, inSize, time):
        dataX = []
        dataY = []
        for i in range(0, len(time_series) - (2 * seq_length)):
            _x = time_series[i:i + seq_length, :inSize]

            _y = time_series[i + seq_length,inSize:]
            # print(_x, "-->",_y)
            dataX.append(_x)
            dataY.append(_y)
        return np.array(dataX), np.array(dataY)

class build__dataset_fromPos_withDir(BuildDatasetBaseClass):
    def build_dataSet(self,time_series, seq_length, inSize, time):
        dataX = []
        dataY = []
        for i in range(0, len(time_series) - (2 * seq_length)):
            _x = time_series[i:i + seq_length, :inSize]
            currentY = time_series[i + seq_length - 1, inSize:]
            futureY = time_series[i + ((seq_length - 1) * 2), inSize:]
            direction = time_series[i + ((seq_length - 1) * 2), 2]
            _y = futureY - currentY
            _y = np.append(_y, direction)
            # print(_x, "-->",_y)
            dataX.append(_x)
            dataY.append(_y)
        return np.array(dataX), np.array(dataY)


class timeseries(Dataset):
    def __init__(self, x, y):
        self.x = x
        self.y = y
        self.len = x.shape[0]

    def __getitem__(self,idx):
        return self.x[idx, :, :],self.y[idx, :]

    def __len__(self):
        return self.len