import numpy as np
import torch
import torch.nn as nn
import torch.optim as optim
from torch.utils.data import DataLoader
from torch.utils.data import Dataset
import numpy as np
import socket
from model import LSTMModel
import os

inSize = 7
learningRate = 0.001
epoch = 100
batchSize = 64
n_hidden = 64
outSize = 2
sequence_length = 50
layerNum = 2

print(torch.__version__)

class oneSeqData():
    def __init__(self):
        self.data = np.zeros((sequence_length, inSize))
        self.count = 0

    def addData(self, string):
        x = string.split(',')
        if(self.count <= sequence_length - 1):
            self.data[self.count] = x.copy()
            self.count = self.count + 1
        else:
            for i in range(0, sequence_length - 1):
                self.data[i] = self.data[i+1].copy()
            self.data[self.count - 1] = x

    def printData(self):
        print("count : " + str(self.count))
        print("data : " + str(self.data))
    def getData(self):
        if(self.count >= sequence_length - 1):
            return self.data
        else:
            print("oneSeqData.getData count error ...")
            return 0


def build_dataset(time_series, seq_length):
    dataX = []
    dataY = []
    for i in range(0, len(time_series) - seq_length):
        _x = time_series[i:i + seq_length, :inSize]
        _y = time_series[seq_length, inSize:]
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


device = torch.device("cuda:0" if torch.cuda.is_available() else "cpu")
print('device : ' + str(device))
lossFunction = nn.MSELoss()
#prepare Model
model = LSTMModel(inSize, n_hidden, outSize, batchSize, epoch, sequence_length, device, lossFunction, learningRate, layerNum).to(device).double()
data = oneSeqData()
modelState = torch.load('./model/model.pkl')
model.load_state_dict(modelState['state_dict'])
model.eval()

# socket
HOST = '127.0.0.1'
PORT = 8000
msgSize = 1024

server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)

server_socket.bind((HOST, PORT))

server_socket.listen()

client_socket, addr = server_socket.accept()



print('Connected by', addr)

resp = client_socket.recv(msgSize)
print("receive ... " + str(resp))
client_socket.sendall(resp)



while True:

    resp = client_socket.recv(msgSize)
    resp = str(resp, 'utf-8')
    #print(resp)
    #resp = resp[1:-1]
    data.addData(resp)
    #data.printData()
    if(data.count >= sequence_length ):
        with torch.no_grad():
            #data.printData()
            x = torch.zeros((1,sequence_length,inSize)).double()
            x[0] = torch.tensor(data.getData())
            output = model.forward(x.to(device))
            output = output[0,sequence_length - 1,:] # use last value for prediction
            #print("output : " + str(output))
            client_socket.sendall(str(str(output[0].item()) +","+str(output[1].item())).encode('utf-8'))
    else:
        client_socket.sendall("0".encode('utf-8'))
